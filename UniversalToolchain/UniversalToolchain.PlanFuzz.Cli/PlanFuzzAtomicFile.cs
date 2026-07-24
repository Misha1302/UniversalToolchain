namespace UniversalToolchain.PlanFuzz.Cli;

internal static class PlanFuzzAtomicFile
{
    public static void WriteAllText(string path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
            Thrower.Argument(nameof(path), "Output path must not be empty.");
        content = content.ArgNotNull();
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath).NotNull());
        var temporaryPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        File.WriteAllText(temporaryPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temporaryPath, fullPath, overwrite: true);
    }

    public static void Copy(string sourcePath, string destinationPath)
    {
        sourcePath = Path.GetFullPath(sourcePath.ArgNotNull());
        destinationPath = Path.GetFullPath(destinationPath.ArgNotNull());
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath).NotNull());
        var temporaryPath = destinationPath + ".tmp-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        File.Copy(sourcePath, temporaryPath, overwrite: true);
        File.Move(temporaryPath, destinationPath, overwrite: true);
    }
}
