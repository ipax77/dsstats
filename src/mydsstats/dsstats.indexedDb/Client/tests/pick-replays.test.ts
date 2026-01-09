import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getReplaysFromFolder } from '../pick-replays';
import * as fileHandleRepository from '../file-handle-repository';

// Mock the File System Access API
const mockFile = (name: string, size: number, lastModified: number): File => ({
    name,
    size,
    lastModified,
    webkitRelativePath: `mock-folder/${name}`,
    type: 'text/plain',
    arrayBuffer: vi.fn(),
    slice: vi.fn(),
    stream: vi.fn(),
    text: vi.fn(),
} as unknown as File);

const mockFileHandle = (name: string, file: File) => ({
    kind: 'file',
    name,
    getFile: vi.fn(() => Promise.resolve(file)),
} as unknown as FileSystemFileHandle);

const mockDirectoryHandle = (name: string, entries: Record<string, any>) => {
    const mockEntries = Object.entries(entries).map(([key, value]) => [key, value]);
    return {
        kind: 'directory',
        name,
        entries: vi.fn(async function* () {
            for (const [key, value] of mockEntries) {
                yield [key, value];
            }
        }),
    } as unknown as FileSystemDirectoryHandle;
};

describe('getReplaysFromFolder', () => {
    let originalShowDirectoryPicker: any;
    let originalWindow: any;

    beforeEach(() => {
        originalShowDirectoryPicker = window.showDirectoryPicker;
        originalWindow = window;

        // Mock file-handle-repository functions

        vi.spyOn(fileHandleRepository, 'saveDirectoryHandle').mockResolvedValue(undefined);
        vi.spyOn(fileHandleRepository, 'verifyDirectoryPermission').mockResolvedValue(true);

        // Mock window.showDirectoryPicker
        Object.defineProperty(window, 'showDirectoryPicker', {
            writable: true,
            value: vi.fn(() => Promise.resolve(
                mockDirectoryHandle('mock-folder', {
                    'replay1.txt': mockFileHandle('replay1.txt', mockFile('replay1.txt', 100, Date.now() - 10000)),
                    'replay2.txt': mockFileHandle('replay2.txt', mockFile('replay2.txt', 200, Date.now() - 20000)),
                    'sub-folder': mockDirectoryHandle('sub-folder', {
                        'replay3.txt': mockFileHandle('replay3.txt', mockFile('replay3.txt', 300, Date.now() - 5000)),
                    }),
                    'other-file.jpg': mockFileHandle('other-file.jpg', mockFile('other-file.jpg', 50, Date.now() - 1000)),
                })
            )),
        });
    });

    afterEach(() => {
        // Restore original window properties
        Object.defineProperty(window, 'showDirectoryPicker', {
            writable: true,
            value: originalShowDirectoryPicker,
        });
        Object.defineProperty(window, 'File', {
            writable: true,
            value: originalWindow.File,
        });
        vi.restoreAllMocks();
    });

    it('should return an empty array if user cancels directory selection', async () => {
        vi.spyOn(fileHandleRepository, 'getDirectoryHandleFromUser').mockResolvedValue(null);
        const result = await getReplaysFromFolder(1, '', [], 10);
        expect(result).toEqual([]);
    });

    it('should return files from the selected directory, filtered by startName and sorted by lastModified', async () => {
        vi.spyOn(fileHandleRepository, 'getDirectoryHandleFromUser').mockResolvedValue(
            mockDirectoryHandle('mock-folder', {
                'replay1.txt': mockFileHandle('replay1.txt', mockFile('replay1.txt', 100, Date.now() - 10000)),
                'replay2.txt': mockFileHandle('replay2.txt', mockFile('replay2.txt', 200, Date.now() - 20000)),
                'sub-folder': mockDirectoryHandle('sub-folder', {
                    'replay3.txt': mockFileHandle('replay3.txt', mockFile('replay3.txt', 300, Date.now() - 5000)),
                }),
                'other-file.jpg': mockFileHandle('other-file.jpg', mockFile('other-file.jpg', 50, Date.now() - 1000)),
            })
        );
        const result = await getReplaysFromFolder(1, 'replay', [], 10);

        expect(result).toHaveLength(3);

        // Expect files to be sorted by lastModified (newest first)
        expect(result[0].name).toBe('replay3.txt');
        expect(result[1].name).toBe('replay1.txt');
        expect(result[2].name).toBe('replay2.txt');

        expect(result[0].path).toBe('mock-folder/sub-folder/replay3.txt');
        expect(result[1].path).toBe('mock-folder/replay1.txt');
        expect(result[2].path).toBe('mock-folder/replay2.txt');
    });

    it('should filter out existing paths', async () => {
        vi.spyOn(fileHandleRepository, 'getDirectoryHandleFromUser').mockResolvedValue(
            mockDirectoryHandle('mock-folder', {
                'replay1.txt': mockFileHandle('replay1.txt', mockFile('replay1.txt', 100, Date.now() - 10000)),
                'replay2.txt': mockFileHandle('replay2.txt', mockFile('replay2.txt', 200, Date.now() - 20000)),
            })
        );
        const existingPaths = ['mock-folder/replay1.txt'];
        const result = await getReplaysFromFolder(1, 'replay', existingPaths, 10);

        expect(result).toHaveLength(1);
        expect(result.some(r => r.name === 'replay1.txt')).toBeFalsy();
    });

    it('should limit the number of returned files by count', async () => {
        vi.spyOn(fileHandleRepository, 'getDirectoryHandleFromUser').mockResolvedValue(
            mockDirectoryHandle('mock-folder', {
                'replay1.txt': mockFileHandle('replay1.txt', mockFile('replay1.txt', 100, Date.now() - 10000)),
                'replay2.txt': mockFileHandle('replay2.txt', mockFile('replay2.txt', 200, Date.now() - 20000)),
                'replay3.txt': mockFileHandle('replay3.txt', mockFile('replay3.txt', 300, Date.now() - 5000)),
            })
        );
        const result = await getReplaysFromFolder(1, 'replay', [], 2);

        expect(result).toHaveLength(2);
        expect(result[0].name).toBe('replay3.txt');
        expect(result[1].name).toBe('replay1.txt');
    });

    it('should use provided dirHandle if available', async () => {
        const getDirectoryHandleFromUserSpy = vi.spyOn(fileHandleRepository, 'getDirectoryHandleFromUser');
        const customDirHandle = mockDirectoryHandle('custom-folder', {
            'custom-replay.txt': mockFileHandle('custom-replay.txt', mockFile('custom-replay.txt', 150, Date.now() - 100)),
        });

        const result = await getReplaysFromFolder(1, 'custom', [], 10, customDirHandle);

        expect(result).toHaveLength(1);
        expect(result[0].name).toBe('custom-replay.txt');
        expect(getDirectoryHandleFromUserSpy).not.toHaveBeenCalled();
        expect(fileHandleRepository.saveDirectoryHandle).not.toHaveBeenCalled();
        expect(fileHandleRepository.verifyDirectoryPermission).toHaveBeenCalledWith(customDirHandle);
    });

    it('should save the directory handle if a new one is selected', async () => {
        vi.spyOn(fileHandleRepository, 'getDirectoryHandleFromUser').mockResolvedValue(
            mockDirectoryHandle('mock-folder', {})
        );
        const result = await getReplaysFromFolder(1, 'replay', [], 10);
        expect(fileHandleRepository.getDirectoryHandleFromUser).toHaveBeenCalled();
        expect(fileHandleRepository.saveDirectoryHandle).toHaveBeenCalledWith('mock-folder_1', expect.any(Object));
    });

    it('should not save the directory handle if dirHandle is provided', async () => {
        const customDirHandle = mockDirectoryHandle('custom-folder', {
            'custom-replay.txt': mockFileHandle('custom-replay.txt', mockFile('custom-replay.txt', 150, Date.now() - 100)),
        });
        await getReplaysFromFolder(1, 'custom', [], 10, customDirHandle);
        expect(fileHandleRepository.saveDirectoryHandle).not.toHaveBeenCalled();
    });

    it('should handle errors during file system access gracefully', async () => {
        vi.spyOn(fileHandleRepository, 'getDirectoryHandleFromUser').mockRejectedValue(new Error('Permission denied'));
        const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {}); // Suppress console.log
        const result = await getReplaysFromFolder(1, 'replay', [], 10);
        expect(result).toEqual([]);
        expect(consoleSpy).toHaveBeenCalledWith('Failed getting file infos: Permission denied');
        consoleSpy.mockRestore();
    });

    describe('chunking', () => {
        const allReplays: Record<string, FileSystemFileHandle> = {};
        for (let i = 0; i < 20; i++) {
            const fileName = `replay${i}.txt`;
            allReplays[fileName] = mockFileHandle(fileName, mockFile(fileName, 100, Date.now() - (20 - i) * 1000));
        }
        const dirHandle = mockDirectoryHandle('chunk-test-folder', allReplays);

        it('should fetch chunks of replays', async () => {
            const dirSpy = vi
                .spyOn(fileHandleRepository, 'getDirectoryHandleFromUser')
                .mockResolvedValue(dirHandle);

            const firstChunk = await getReplaysFromFolder(1, 'replay', [], 10);

            const newReplays = {
                ...allReplays,
                'replay-new-1.txt': mockFileHandle(
                    'replay-new-1.txt',
                    mockFile('replay-new-1.txt', 150, Date.now() - 500)
                ),
                'replay-new-2.txt': mockFileHandle(
                    'replay-new-2.txt',
                    mockFile('replay-new-2.txt', 150, Date.now() - 200)
                ),
            };

            const updatedDirHandle = mockDirectoryHandle('chunk-test-folder', newReplays);
            dirSpy.mockResolvedValue(updatedDirHandle);

            const firstChunkPaths = firstChunk.map(m => m.path);
            const secondChunk = await getReplaysFromFolder(1, 'replay', firstChunkPaths, 10);

            expect(secondChunk[0].name).toBe('replay-new-2.txt');
            expect(secondChunk[1].name).toBe('replay-new-1.txt');

            const secondChunkPaths = [...firstChunkPaths, ...secondChunk.map(m => m.path)];
            const thirdChunk = await getReplaysFromFolder(1, 'replay', secondChunkPaths, 10);
            expect(thirdChunk).toHaveLength(2);
        });
    });
});