using System.Configuration;

namespace NServiceBus.Config
{

    public class RijndaelEncryptionServiceConfig : ConfigurationSection
    {
        /// <summary>
        /// The encryption key.
        /// </summary>
        [ConfigurationProperty("Key", IsRequired = true)]
        public string Key
        {
            get
            {
                return this["Key"] as string;
            }
            set
            {
                this["Key"] = value;
            }
        }
        /// <summary>
        /// Contains the expired decryptions that are currently being phased out.
        /// </summary>
        [ConfigurationProperty("ExpiredKeys", IsRequired = false)]
        public RijndaelExpiredKeyCollection ExpiredKeys
        {
            get
            {
                return this["ExpiredKeys"] as RijndaelExpiredKeyCollection;
            }
            set
            {
                this["ExpiredKeys"] = value;
            }
        }
        /// <summary>
        /// The encryption key identfier used for decryption.
        /// </summary>
        [ConfigurationProperty("KeyIdentifier", IsRequired = false)]
        public string KeyIdentifier
        {
            get
            {
                return this["KeyIdentifier"] as string;
            }
            set
            {
                this["KeyIdentifier"] = value;
            }
        }
    }
}
