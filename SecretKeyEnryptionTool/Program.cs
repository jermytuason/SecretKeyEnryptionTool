using Ionic.Zip;
using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SecretKeyEnryptionTool
{
    class Program
    {
        private static Configuration config;
        private static string passwordVal = "";
        private static string secretKeyOneVal = "";
        private static string secretKeyTwoVal = "";
        private static string encryptPath = "";
        private static string zipFolderPath = "";
        private static string zipPath = "";
        private static string configPath = "";

        public static void Main() => Run();

        static async Task Run()
        {
            Console.WriteLine("Secret Key Encryption Tool");
            Console.WriteLine("");

            var secretKeyOne = "Please enter Secret Key 1: ";
            CheckSecretKeyOne(secretKeyOne);

            var secretKeyTwo = "Please enter Secret Key 2: ";
            CheckSecretKeyTwo(secretKeyTwo);

            var password = "Please enter Password: ";
            CheckPassword(password);

            // Checker if the values are correct. Comment this out before deploying
            //Console.WriteLine("");
            //Console.WriteLine(string.Format("{0}", secretKeyOneVal));
            //Console.WriteLine(string.Format("{0}", secretKeyTwoVal));
            //Console.WriteLine(string.Format("{0}", passwordVal));

            Console.WriteLine("");

            if (!string.IsNullOrEmpty(secretKeyOneVal) && !string.IsNullOrEmpty(secretKeyTwoVal) && !string.IsNullOrEmpty(passwordVal))
            {
                try
                {
                    config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    encryptPath = config.AppSettings.Settings["encryptPath"].Value;
                    zipFolderPath = config.AppSettings.Settings["zipFolderPath"].Value;
                    zipPath = config.AppSettings.Settings["zipPath"].Value;
                    configPath = config.AppSettings.Settings["configPath"].Value;

                    #region Encrypt Secret Key

                    EncryptSecretKey(secretKeyOneVal, secretKeyTwoVal);

                    #endregion

                    #region Zip File with Password

                    ZipFileWithPassword(passwordVal);

                    #endregion

                    #region Encrypt Password

                    var encryptedPassword = EncryptPassword(passwordVal);

                    #endregion

                    #region Store Password and Path to config file

                    await SavePasswordAndPath(encryptedPassword);

                    #endregion

                    #region Delete .DAT file in the directory

                    DeleteEncryptedDATFile();

                    #endregion
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("System has encountered an error: {0}", ex.Message));
                }
            }
            else
            {
                Console.WriteLine("");
                Console.WriteLine("Secret Key 1, Secret Key 2 or Password is Empty or Null. Please try again.");
                Console.WriteLine("");
            }

            Console.WriteLine("");
            Console.WriteLine("Secret Key Encryption is now done. Press any key to close the tool. ");
            var end = Console.ReadLine();
            Console.WriteLine("");
        }


        #region Encrypt Methods

        static void EncryptSecretKey(string secretKeyOne, string secretKeyTwo)
        {
            Console.WriteLine("Encrypting Secret Key has now started.....");

            var secretKey = string.Format("{0}{1}", secretKeyOne, secretKeyTwo);

            // Create the original data to be encrypted
            var toEncrypt = UnicodeEncoding.ASCII.GetBytes(secretKey);

            // Create a file.
            var fStream = new FileStream(encryptPath, FileMode.OpenOrCreate);

            var scope = (DataProtectionScope)Enum.Parse(typeof(DataProtectionScope), config.AppSettings.Settings["dataProtectionScope"].Value);

            // Encrypt a copy of the data to the stream.
            var bytesWritten = EncryptDataToStream(toEncrypt, scope, fStream);

            fStream.Close();

            Console.WriteLine("Encrypting Secret Key has now ended.....");
            Console.WriteLine();
        }

        static string EncryptPassword(string password)
        {
            Console.WriteLine("Encrypting Password has now started...");

            // Create some random entropy.

            if (password == null) throw new ArgumentNullException(nameof(password));

            var scope = (DataProtectionScope)Enum.Parse(typeof(DataProtectionScope), config.AppSettings.Settings["dataProtectionScope"].Value);

            byte[] clearBytes = Encoding.UTF8.GetBytes(password);
            byte[] encryptedBytes = ProtectedData.Protect(clearBytes, null, scope);

            Console.WriteLine("Encrypting Password has now ended...");
            Console.WriteLine("");

            return Convert.ToBase64String(encryptedBytes);
        }

        static int EncryptDataToStream(byte[] Buffer, DataProtectionScope Scope, Stream S)
        {
            int length = 0;

            // Encrypt the data and store the result in a new byte array. The original data remains unchanged.
            byte[] encryptedData = ProtectedData.Protect(Buffer, null, Scope);

            // Write the encrypted data to a stream.
            if (S.CanWrite && encryptedData != null)
            {
                S.Write(encryptedData, 0, encryptedData.Length);

                length = encryptedData.Length;
            }

            // Return the length that was written to the stream.
            return length;
        }

        #endregion

        #region Other Methods

        static void ZipFileWithPassword(string password)
        {
            Console.WriteLine("Zipping the file with Password has now started...");

            var zip = new ZipFile();

            zip.Password = password;
            zip.AddDirectory(zipFolderPath);
            zip.Save(zipPath);

            Console.WriteLine("Zipping the file with Password has now ended...");
            Console.WriteLine();
        }

        static async Task SavePasswordAndPath(string encryptedPassword)
        {
            Console.WriteLine("Saving Zip File and Encrypted Password in File Server has now started...");

            string[] lines =
            {
                zipPath, encryptedPassword
            };

            await File.WriteAllLinesAsync(configPath, lines);

            Console.WriteLine("Saving Zip File and Encrypted Password in File Server has now ended...");
            Console.WriteLine("");
        }

        static void DeleteEncryptedDATFile()
        {
            try
            {
                Console.WriteLine("Deleting .DAT file in directory has now started...");

                // Check if file exists with its full path    
                if (File.Exists(encryptPath))
                {
                    // If file found, delete it    
                    File.Delete(encryptPath);
                }

                Console.WriteLine("Deleting .DAT file in directory has now ended...");
                Console.WriteLine("");
            }
            catch (IOException ioExp)
            {
                Console.WriteLine(ioExp.Message);
            }
        }

        #endregion

        #region Checker for Input for Asterisk

        static void CheckSecretKeyOne(string secretKeyOne)
        {
            try
            {
                Console.Write(secretKeyOne);
                secretKeyOneVal = "";
                do
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    // Backspace Should Not Work  
                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        secretKeyOneVal += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && secretKeyOneVal.Length > 0)
                        {
                            secretKeyOneVal = secretKeyOneVal.Substring(0, (secretKeyOneVal.Length - 1));
                            Console.Write("\b \b");
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            if (string.IsNullOrWhiteSpace(secretKeyOneVal))
                            {
                                Console.WriteLine("");
                                Console.WriteLine("Empty value not allowed.");
                                CheckSecretKeyOne(secretKeyOne);
                                break;
                            }
                            else
                            {
                                Console.WriteLine("");
                                break;
                            }
                        }
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        static void CheckSecretKeyTwo(string secretKeyTwo)
        {
            try
            {
                Console.Write(secretKeyTwo);
                secretKeyTwoVal = "";
                do
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    // Backspace Should Not Work  
                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        secretKeyTwoVal += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && secretKeyTwoVal.Length > 0)
                        {
                            secretKeyTwoVal = secretKeyTwoVal.Substring(0, (secretKeyTwoVal.Length - 1));
                            Console.Write("\b \b");
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            if (string.IsNullOrWhiteSpace(secretKeyTwoVal))
                            {
                                Console.WriteLine("");
                                Console.WriteLine("Empty value not allowed.");
                                CheckSecretKeyTwo(secretKeyTwo);
                                break;
                            }
                            else
                            {
                                Console.WriteLine("");
                                break;
                            }
                        }
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        static void CheckPassword(string EnterText)
        {
            try
            {
                Console.Write(EnterText);
                passwordVal = "";
                do
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    // Backspace Should Not Work  
                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        passwordVal += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && passwordVal.Length > 0)
                        {
                            passwordVal = passwordVal.Substring(0, (passwordVal.Length - 1));
                            Console.Write("\b \b");
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            if (string.IsNullOrWhiteSpace(passwordVal))
                            {
                                Console.WriteLine("");
                                Console.WriteLine("Empty value not allowed.");
                                CheckPassword(EnterText);
                                break;
                            }
                            else
                            {
                                Console.WriteLine("");
                                break;
                            }
                        }
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}
