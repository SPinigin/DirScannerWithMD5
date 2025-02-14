using System.Collections.Concurrent;
using System.Security.Cryptography;

class DirScannerWithMD5
{
    static void Main(string[] args)
    {
        string pathToScan = "D:\\2. Магистратура";
        string outputFile = "D:\\4. Работа\\ТННЦ\\MD5.txt";

        var existingFiles = ReadFileDataFromFile(outputFile);
        List<FileData> filesData = ScanDir(pathToScan, "*.*");
        UpdateFileData(existingFiles, filesData, outputFile);
    }

    static List<FileData> ScanDir(string dirForScan, string searchPattern)
    {
        var filesData = new ConcurrentBag<FileData>();
        var dirs = new Stack<string>();
        dirs.Push(dirForScan);

        while (dirs.Count > 0)
        {
            string currentDir = dirs.Pop();
            try
            {
                string[] files = Directory.GetFiles(currentDir, searchPattern);
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 5 }, (file) =>
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        string hashMD5 = GetMD5(file);
                        filesData.Add(new FileData
                        {
                            rel_path = file,
                            file_size = fileInfo.Length,
                            hash_md5 = hashMD5
                        });

                        Console.WriteLine(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при обработке файла {file}: {ex.Message}");
                    }
                });

                string[] subDirs = Directory.GetDirectories(currentDir);
                foreach (string subDir in subDirs)
                {
                    dirs.Push(subDir);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Ошибка доступа к директории {currentDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке директории {currentDir}: {ex.Message}");
            }
        }

        return new List<FileData>(filesData);
    }

    public static string GetMD5(string filePath)
    {
        using var md5 = MD5.Create();
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", "");
    }

    public static void WriteFileDataToFile(List<FileData> filesData, string outputFile)
    {
        using StreamWriter writer = new StreamWriter(outputFile);
        foreach (var file in filesData)
        {
            writer.WriteLine($"{file.rel_path} {file.hash_md5} {file.file_size}");
        }
    }

    public static List<FileData> ReadFileDataFromFile(string outputFile)
    {
        var filesData = new List<FileData>();

        if (File.Exists(outputFile))
        {
            foreach (var line in File.ReadAllLines(outputFile))
            {
                try
                {
                    var parts = line.Split(' ');

                    if (parts.Length == 3)
                    {
                        filesData.Add(new FileData
                        {
                            rel_path = parts[0],
                            hash_md5 = parts[1],
                            file_size = long.Parse(parts[2])
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Строка в файле не соответствует ожидаемому формату: {line}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении строки из файла: {line}. Ошибка: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine($"Файл {outputFile} не найден.");
        }

        return filesData;
    }

    public static void UpdateFileData(List<FileData> existingFiles, List<FileData> currentFiles, string outputFile)
    {
        var updatedFiles = new List<FileData>();

        foreach (var currentFile in currentFiles)
        {
            var existingFile = existingFiles.FirstOrDefault(f => f.rel_path == currentFile.rel_path);
            if (existingFile == null || existingFile.hash_md5 != currentFile.hash_md5)
            {
                updatedFiles.Add(currentFile);
            }
        }

        foreach (var existingFile in existingFiles)
        {
            if (!updatedFiles.Any(f => f.rel_path == existingFile.rel_path) &&
                currentFiles.Any(f => f.rel_path == existingFile.rel_path))
            {
                updatedFiles.Add(existingFile);
            }
        }

        WriteFileDataToFile(updatedFiles, outputFile);
    }
    public class FileData
    {
        public string rel_path;
        public long file_size;
        public string hash_md5 = string.Empty;

        public override string ToString()
        {
            return $"{rel_path} {file_size} {hash_md5}";
        }
    }
}
