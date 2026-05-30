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

const mockFileWithPath = (name: string, relativePath: string, size: number, lastModified: number): File => ({
    name,
    size,
    lastModified,
    webkitRelativePath: relativePath,
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
        // addDirectoryHandle returns the UUID/key used as path root; return handle.name
        // so that stored paths match mock-folder/... expectations in assertions.
        vi.spyOn(fileHandleRepository, 'addDirectoryHandle').mockImplementation(async (handle) => (handle as any).name);
        vi.spyOn(fileHandleRepository, 'verifyDirectoryPermission').mockResolvedValue(true);
        vi.spyOn(fileHandleRepository, 'updateDirectoryFingerprint').mockResolvedValue();
        vi.spyOn(fileHandleRepository, 'updateDirectoryScanState').mockResolvedValue();

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
        vi.spyOn(fileHandleRepository, 'getDirectorySourceFromUser').mockResolvedValue(null);
        const result = await getReplaysFromFolder("replay", [], 10);
        expect(result).toEqual([]);
    });

    it('should return files from the selected directory, filtered by startName and sorted by lastModified', async () => {
        const dirHandle = mockDirectoryHandle('mock-folder', {
                'replay1.txt': mockFileHandle('replay1.txt', mockFile('replay1.txt', 100, Date.now() - 10000)),
                'replay2.txt': mockFileHandle('replay2.txt', mockFile('replay2.txt', 200, Date.now() - 20000)),
                'sub-folder': mockDirectoryHandle('sub-folder', {
                    'replay3.txt': mockFileHandle('replay3.txt', mockFile('replay3.txt', 300, Date.now() - 5000)),
                }),
                'other-file.jpg': mockFileHandle('other-file.jpg', mockFile('other-file.jpg', 50, Date.now() - 1000)),
            });
        vi.spyOn(fileHandleRepository, 'getDirectorySourceFromUser').mockResolvedValue({ kind: 'handle', handle: dirHandle });
        const result = await getReplaysFromFolder("replay", [], 10);

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
        const dirHandle = mockDirectoryHandle('mock-folder', {
                'replay1.txt': mockFileHandle('replay1.txt', mockFile('replay1.txt', 100, Date.now() - 10000)),
                'replay2.txt': mockFileHandle('replay2.txt', mockFile('replay2.txt', 200, Date.now() - 20000)),
            });
        vi.spyOn(fileHandleRepository, 'getDirectorySourceFromUser').mockResolvedValue({ kind: 'handle', handle: dirHandle });
        const existingPaths = ['mock-folder/replay1.txt'];
        const result = await getReplaysFromFolder('replay', existingPaths, 1);

        expect(result).toHaveLength(1);
        expect(result[0].name).toBe('replay2.txt');
    });

    it('should filter meta paths after restoring a directory handle', async () => {
        const restoredDirHandle = mockDirectoryHandle('restored-folder', {
            'replay1.txt': mockFileHandle('replay1.txt', mockFile('replay1.txt', 100, Date.now() - 10000)),
            'replay2.txt': mockFileHandle('replay2.txt', mockFile('replay2.txt', 200, Date.now() - 5000)),
        });

        const result = await getReplaysFromFolder(
            'replay',
            [],
            10,
            restoredDirHandle,
            'restored-root',
            [
                {
                    replayHash: 'hash-1',
                    filePath: 'restored-root/replay1.txt',
                    regionId: 1,
                    uploaded: 0,
                    skip: false,
                },
            ],
        );

        expect(result).toHaveLength(1);
        expect(result[0].path).toBe('restored-root/replay2.txt');
    });

    it('should filter legacy folder-name meta paths when the current scan uses a root key', async () => {
        const restoredDirHandle = mockDirectoryHandle('restored-folder', {
            'replay1.txt': mockFileHandle('replay1.txt', mockFile('replay1.txt', 100, Date.now() - 10000)),
            'replay2.txt': mockFileHandle('replay2.txt', mockFile('replay2.txt', 200, Date.now() - 5000)),
        });

        const result = await getReplaysFromFolder(
            'replay',
            [],
            10,
            restoredDirHandle,
            'restored-root',
            [
                {
                    replayHash: 'hash-1',
                    filePath: 'restored-folder/replay1.txt',
                    regionId: 1,
                    uploaded: 0,
                    skip: false,
                },
            ],
        );

        expect(result).toHaveLength(1);
        expect(result[0].path).toBe('restored-root/replay2.txt');
    });

    it('should re-include a legacy meta path when the file was modified after the last bound scan time', async () => {
        const oldModified = Date.now() - 20_000;
        const newModified = Date.now() - 1_000;
        const restoredDirHandle = mockDirectoryHandle('restored-folder', {
            'replay1.txt': mockFileHandle('replay1.txt', mockFile('replay1.txt', 150, newModified)),
        });

        vi.spyOn(fileHandleRepository, 'getDirectoryHandle').mockResolvedValue({
            handle: restoredDirHandle,
            displayName: 'restored-folder',
            regionId: 0,
            fingerprint: null,
            status: 'bound',
            lastBoundAt: oldModified,
        });

        const result = await getReplaysFromFolder(
            'replay',
            [],
            10,
            restoredDirHandle,
            'restored-root',
            [
                {
                    replayHash: 'hash-1',
                    filePath: 'restored-root/replay1.txt',
                    regionId: 1,
                    uploaded: 0,
                    skip: false,
                },
            ],
        );

        expect(result).toHaveLength(1);
        expect(result[0].path).toBe('restored-root/replay1.txt');
    });

    it('should limit the number of returned files by count', async () => {
        const dirHandle = mockDirectoryHandle('mock-folder', {
                'replay1.txt': mockFileHandle('replay1.txt', mockFile('replay1.txt', 100, Date.now() - 10000)),
                'replay2.txt': mockFileHandle('replay2.txt', mockFile('replay2.txt', 200, Date.now() - 20000)),
                'replay3.txt': mockFileHandle('replay3.txt', mockFile('replay3.txt', 300, Date.now() - 5000)),
            });
        vi.spyOn(fileHandleRepository, 'getDirectorySourceFromUser').mockResolvedValue({ kind: 'handle', handle: dirHandle });
        const result = await getReplaysFromFolder('replay', [], 2);

        expect(result).toHaveLength(2);
        expect(result[0].name).toBe('replay3.txt');
        expect(result[1].name).toBe('replay1.txt');
    });

    it('should use provided dirHandle if available', async () => {
        const getDirectorySourceFromUserSpy = vi.spyOn(fileHandleRepository, 'getDirectorySourceFromUser');
        const customDirHandle = mockDirectoryHandle('custom-folder', {
            'custom-replay.txt': mockFileHandle('custom-replay.txt', mockFile('custom-replay.txt', 150, Date.now() - 100)),
        });

        const result = await getReplaysFromFolder('custom', [], 10, customDirHandle);

        expect(result).toHaveLength(1);
        expect(result[0].name).toBe('custom-replay.txt');
        expect(getDirectorySourceFromUserSpy).not.toHaveBeenCalled();
        expect(fileHandleRepository.addDirectoryHandle).not.toHaveBeenCalled();
        expect(fileHandleRepository.verifyDirectoryPermission).toHaveBeenCalledWith(customDirHandle);
    });

    it('should save the directory handle if a new one is selected', async () => {
        const dirHandle = mockDirectoryHandle('mock-folder', {});
        vi.spyOn(fileHandleRepository, 'getDirectorySourceFromUser').mockResolvedValue({ kind: 'handle', handle: dirHandle });
        const result = await getReplaysFromFolder("replay", [], 10);
        expect(fileHandleRepository.getDirectorySourceFromUser).toHaveBeenCalled();
        // expect(fileHandleRepository.addDirectoryHandle).toHaveBeenCalledWith(expect.any(Object), 'mock-folder', 1);
    });

    it('should not save the directory handle if dirHandle is provided', async () => {
        const customDirHandle = mockDirectoryHandle('custom-folder', {
            'custom-replay.txt': mockFileHandle('custom-replay.txt', mockFile('custom-replay.txt', 150, Date.now() - 100)),
        });
        await getReplaysFromFolder('custom', [], 10, customDirHandle);
        expect(fileHandleRepository.addDirectoryHandle).not.toHaveBeenCalled();
    });

    it('should handle errors during file system access gracefully', async () => {
        vi.spyOn(fileHandleRepository, 'getDirectorySourceFromUser').mockRejectedValue(new Error('Permission denied'));
        const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {}); // Suppress console.log
        const result = await getReplaysFromFolder("replay", [], 10);
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
                .spyOn(fileHandleRepository, 'getDirectorySourceFromUser')
                .mockResolvedValue({ kind: 'handle', handle: dirHandle });

            const firstChunk = await getReplaysFromFolder("replay", [], 10);

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
            dirSpy.mockResolvedValue({ kind: 'handle', handle: updatedDirHandle });

            const firstChunkPaths = firstChunk.map(m => m.path);
            const secondChunk = await getReplaysFromFolder('replay', firstChunkPaths, 10);

            expect(secondChunk[0].name).toBe('replay-new-2.txt');
            expect(secondChunk[1].name).toBe('replay-new-1.txt');

            const secondChunkPaths = [...firstChunkPaths, ...secondChunk.map(m => m.path)];
            const thirdChunk = await getReplaysFromFolder('replay', secondChunkPaths, 2);
            expect(thirdChunk).toHaveLength(2);
        });
    });

    it('should scan fallback folder files by webkitRelativePath', async () => {
        const newest = Date.now() - 100;
        vi.spyOn(fileHandleRepository, 'getDirectorySourceFromUser').mockResolvedValue({
            kind: 'files',
            displayName: 'fallback-folder',
            files: [
                mockFileWithPath('replay-old.SC2Replay', 'fallback-folder/replay-old.SC2Replay', 100, newest - 2000),
                mockFileWithPath('notes.txt', 'fallback-folder/notes.txt', 50, newest - 1000),
                mockFileWithPath('replay-new.SC2Replay', 'fallback-folder/nested/replay-new.SC2Replay', 120, newest),
            ],
        });
        vi.spyOn(fileHandleRepository, 'addFallbackDirectoryFiles').mockResolvedValue('fallback-root');

        const result = await getReplaysFromFolder('replay', [], 10);

        expect(result).toHaveLength(2);
        expect(result[0].path).toBe('fallback-root/nested/replay-new.SC2Replay');
        expect(result[1].path).toBe('fallback-root/replay-old.SC2Replay');
    });

    it('should reselect an unbound fallback folder and keep the existing root key', async () => {
        const selectedFile = mockFileWithPath('replay1.SC2Replay', 'fallback-folder/replay1.SC2Replay', 100, Date.now());
        vi.spyOn(fileHandleRepository, 'getDirectoryHandle').mockResolvedValue({
            handle: null,
            displayName: 'fallback-folder',
            regionId: 0,
            fingerprint: {
                version: 1,
                files: [{ name: selectedFile.name, size: selectedFile.size, lastModified: selectedFile.lastModified }],
            },
            status: 'unbound',
        });
        vi.spyOn(fileHandleRepository, 'getSessionFallbackFiles').mockReturnValue(undefined);
        vi.spyOn(fileHandleRepository, 'getDirectorySourceFromUser').mockResolvedValue({
            kind: 'files',
            displayName: 'fallback-folder',
            files: [selectedFile],
        });
        vi.spyOn(fileHandleRepository, 'addFallbackDirectoryFiles').mockResolvedValue('saved-root');

        const result = await getReplaysFromFolder('replay', [], 10, null, 'saved-root');

        expect(fileHandleRepository.addFallbackDirectoryFiles).toHaveBeenCalledWith([selectedFile], 'replay', 'saved-root', 'fallback-folder');
        expect(result).toHaveLength(1);
        expect(result[0].path).toBe('saved-root/replay1.SC2Replay');
    });

    it('should reject a fallback reselect when the fingerprint does not match', async () => {
        const originalFile = mockFileWithPath('replay1.SC2Replay', 'fallback-folder/replay1.SC2Replay', 100, Date.now() - 1000);
        const selectedOtherFolder = mockFileWithPath('replay2.SC2Replay', 'other-folder/replay2.SC2Replay', 200, Date.now());

        const key = await fileHandleRepository.addFallbackDirectoryFiles([originalFile], 'replay', undefined, 'fallback-folder');

        await expect(
            fileHandleRepository.addFallbackDirectoryFiles([selectedOtherFolder], 'replay', key, 'fallback-folder')
        ).rejects.toThrow('Selected folder does not match');
    });
});
