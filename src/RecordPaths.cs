using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DailyCaravan
{
    internal sealed class RecordPaths
    {
        private readonly string directory, profile, runHash, day;

        internal RecordPaths(string config, int profile, string runHash, int day)
        {
            directory = Path.Combine(config, "DailyCaravan");
            this.profile = profile.ToString(CultureInfo.InvariantCulture);
            this.runHash = runHash;
            this.day = day.ToString(CultureInfo.InvariantCulture);
        }

        internal string For(string checkpoint)
        {
            // The game's Windows Mono runtime can fail to open a 260-character path.
            // Keep all 256 digest bits while leaving room for the atomic write suffix.
            using var sha = SHA256.Create();
            var key = Encoding.UTF8.GetBytes(string.Join("|", profile, runHash, day, checkpoint));
            string name = BitConverter.ToString(sha.ComputeHash(key)).Replace("-", "");
            return Path.Combine(directory, name + ".json");
        }

        internal string Legacy(string checkpoint) => Path.Combine(directory, profile, runHash, day, checkpoint + ".json");
    }
}
