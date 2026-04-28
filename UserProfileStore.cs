using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace VerhozinaIvanovDiplom
{
    public static class UserProfileStore
    {
        private static readonly string StorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VerhozinaIvanovDiplom",
            "user_profiles.json");

        public static UserProfileData Get(int userId)
        {
            var all = LoadAll();
            if (all.TryGetValue(userId, out var profile))
            {
                return profile;
            }

            return new UserProfileData();
        }

        public static void Save(int userId, UserProfileData profile)
        {
            var all = LoadAll();
            all[userId] = profile ?? new UserProfileData();
            SaveAll(all);
        }

        private static Dictionary<int, UserProfileData> LoadAll()
        {
            try
            {
                if (!File.Exists(StorePath))
                {
                    return new Dictionary<int, UserProfileData>();
                }

                using (var stream = File.OpenRead(StorePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(Dictionary<int, UserProfileData>));
                    return serializer.ReadObject(stream) as Dictionary<int, UserProfileData>
                        ?? new Dictionary<int, UserProfileData>();
                }
            }
            catch
            {
                return new Dictionary<int, UserProfileData>();
            }
        }

        private static void SaveAll(Dictionary<int, UserProfileData> all)
        {
            var directory = Path.GetDirectoryName(StorePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = File.Create(StorePath))
            {
                var serializer = new DataContractJsonSerializer(typeof(Dictionary<int, UserProfileData>));
                serializer.WriteObject(stream, all);
            }
        }
    }

    [DataContract]
    public sealed class UserProfileData
    {
        [DataMember]
        public int? Age { get; set; }

        [DataMember]
        public string Position { get; set; }

        [DataMember]
        public string PhotoBase64 { get; set; }
    }
}
