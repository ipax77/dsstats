﻿namespace dsstats.foldertest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var sc2Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Starcraft II");
        Console.WriteLine($"sc2 dir: {sc2Dir}");
        if (!Directory.Exists(sc2Dir))
        {
            Console.WriteLine("sc2 dir does not exist.");
            return;
        }

        foreach (var file in Directory.GetFiles(sc2Dir, "*.lnk", SearchOption.TopDirectoryOnly))
        {
            var target = GetShortcutTarget(file);
            Console.WriteLine($"found target for {file}: {target}");
        }
    }

    private static string? GetShortcutTarget(string file)
    {
        try
        {
            if (Path.GetExtension(file).ToLower() != ".lnk")
            {
                return null;
            }

            FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
            using (BinaryReader fileReader = new BinaryReader(fileStream))
            {
                fileStream.Seek(0x14, SeekOrigin.Begin);     // Seek to flags
                uint flags = fileReader.ReadUInt32();        // Read flags
                if ((flags & 1) == 1)
                {                      // Bit 1 set means we have to
                                       // skip the shell item ID list
                    fileStream.Seek(0x4c, SeekOrigin.Begin); // Seek to the end of the header
                    uint offset = fileReader.ReadUInt16();   // Read the length of the Shell item ID list
                    fileStream.Seek(offset, SeekOrigin.Current); // Seek past it (to the file locator info)
                }

                long fileInfoStartsAt = fileStream.Position; // Store the offset where the file info
                                                             // structure begins
                uint totalStructLength = fileReader.ReadUInt32(); // read the length of the whole struct
                fileStream.Seek(0xc, SeekOrigin.Current); // seek to offset to base pathname
                uint fileOffset = fileReader.ReadUInt32(); // read offset to base pathname
                                                           // the offset is from the beginning of the file info struct (fileInfoStartsAt)
                fileStream.Seek((fileInfoStartsAt + fileOffset), SeekOrigin.Begin); // Seek to beginning of
                                                                                    // base pathname (target)
                long pathLength = (totalStructLength + fileInfoStartsAt) - fileStream.Position - 2; // read
                                                                                                    // the base pathname. I don't need the 2 terminating nulls.
                char[] linkTarget = fileReader.ReadChars((int)pathLength); // should be unicode safe
                var link = new string(linkTarget);

                int begin = link.IndexOf("\0\0");
                if (begin > -1)
                {
                    int end = link.IndexOf("\\\\", begin + 2) + 2;
                    end = link.IndexOf('\0', end) + 1;

                    string firstPart = link[..begin];
                    string secondPart = link[end..];

                    return firstPart + secondPart;
                }
                else
                {
                    return link;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Faild getting shortcut path: {ex.Message}");
            return null;
        }
    }
}
