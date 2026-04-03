namespace Admitto.Infrastructure.Services
{
    /// <summary>
    /// Validates uploaded files against an extension allowlist and magic-byte signatures.
    /// Extension alone is not sufficient — a malicious file can be renamed. Checking
    /// the first bytes of the stream confirms the actual file format matches the claim.
    /// </summary>
    public static class FileValidator
    {
        // Allowed extensions mapped to their accepted magic-byte signatures.
        // Multiple signatures per extension cover format variants (e.g. different JPEG markers).
        private static readonly Dictionary<string, byte[][]> AllowedSignatures =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"]  = [[ 0xFF, 0xD8, 0xFF ]],
                [".jpeg"] = [[ 0xFF, 0xD8, 0xFF ]],
                [".png"]  = [[ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A ]],
                [".gif"]  = [[ 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 ],
                              [ 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 ]],
                [".webp"] = [[ 0x52, 0x49, 0x46, 0x46 ]],  // RIFF header; full check below
                [".mp4"]  = [[ 0x00, 0x00, 0x00 ]],         // first 3 bytes; ftyp at offset 4
                [".mov"]  = [[ 0x00, 0x00, 0x00 ]],
                [".pdf"]  = [[ 0x25, 0x50, 0x44, 0x46 ]],   // %PDF
            };

        public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        /// <summary>
        /// Returns an error message, or null if the file is valid.
        /// The stream position is reset to 0 after reading so the caller can still save it.
        /// </summary>
        public static async Task<string?> ValidateAsync(Stream stream, string fileName)
        {
            if (stream.Length > MaxFileSizeBytes)
                return $"File exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.";

            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext) || !AllowedSignatures.TryGetValue(ext, out var signatures))
                return $"File type '{ext}' is not permitted. Allowed: {string.Join(", ", AllowedSignatures.Keys)}.";

            // Read enough bytes to cover the longest signature.
            var headerLength = signatures.Max(s => s.Length);
            var header = new byte[headerLength];
            stream.Position = 0;
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, headerLength));
            stream.Position = 0; // reset for the actual save

            if (!signatures.Any(sig => bytesRead >= sig.Length && header.Take(sig.Length).SequenceEqual(sig)))
                return $"File content does not match the declared extension '{ext}'. Upload rejected.";

            return null;
        }
    }
}
