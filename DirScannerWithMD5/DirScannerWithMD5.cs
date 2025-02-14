using System.Collections.Concurrent;
using System.Security.Cryptography;

class DirScannerWithMD5
{
    static void Main(string[] args)
    {
        string pathToScan = "";

        ScanDir(pathToScan, "*.*");


    }

    static List<File> ScanDir(string dirForScan, string searchPattern)
    {
        var filesData = new ConcurrentBag<File>();
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
                        filesData.Add(new File
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
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке директории {currentDir}: {ex.Message}");
            }
        }

        return new List<File>(filesData);
    }

    public static string GetMD5(string filePath)
    {
        using var md5 = MD5.Create();
        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", "");
    }

    public class File
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
