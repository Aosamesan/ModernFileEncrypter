using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ModernFileEncrypter.ViewModels
{
    public class DecryptViewModel : NotifyPropertyChanged
    {
        #region Decrypter
        private RSA.RSAByteEncrypter Decrypter { get; }
        #endregion

        #region Show Message Box
        public event ShowMessageBox Completed;
        #endregion

        #region Idle
        private bool isIdle;
        public bool IsIdle
        {
            get { return isIdle; }
            set { isIdle = value; RaisePropertyChanged(nameof(IsIdle), nameof(CanDecrypt)); }
        }
        #endregion

        #region File Pathes
        private string encryptedFilePath;
        public string EncryptedFilePath
        {
            get { return encryptedFilePath; }
            set { encryptedFilePath = value; RaisePropertyChanged(nameof(EncryptedFilePath), nameof(CanDecrypt)); }
        }
        private string keyFilePath;
        public string KeyFilePath
        {
            get { return keyFilePath; }
            set { keyFilePath = value; RaisePropertyChanged(nameof(KeyFilePath), nameof(CanDecrypt)); }
        }

        public ICommand SelectFileCommand { get; }
        public ICommand SelectKeyCommand { get; }
        #endregion

        #region Decrypt
        private string saveFilePath;
        public ICommand DecryptCommand { get; }
        private string decryptMessage;
        Helper.ProgressTimeHelper timeHelper;
        public string DecryptMessage
        {
            get { return decryptMessage; }
            set { decryptMessage = value; RaisePropertyChanged(nameof(DecryptMessage)); }
        }
        public bool CanDecrypt
        {
            get
            {
                return IsIdle && File.Exists(EncryptedFilePath) && File.Exists(KeyFilePath);
            }
        }

        private async Task Decrypt()
        {
            FileInfo file = new FileInfo(EncryptedFilePath);
            FileInfo key = new FileInfo(KeyFilePath);

            var progress = await Set(file.Length);
            using (cancelTokenSource = new CancellationTokenSource())
            {
                IsIdle = false;
                try
                {
                    await Decrypt(file, key, progress, cancelTokenSource.Token);
                    Reset();
                }
                catch (OperationCanceledException e)
                {
                    DecryptMessage = "작업이 취소되었습니다.";
                    await Task.Delay(1);
                    if (File.Exists(saveFilePath))
                        File.Delete(saveFilePath);
                }
                catch (Exception e)
                {
                    DecryptMessage = $"에러 발생! {e.Message}";
                    await Task.Delay(500);
                    if (File.Exists(saveFilePath))
                        File.Delete(saveFilePath);
                }
                IsIdle = true;
                CommandManager.InvalidateRequerySuggested();
            }

        }

        private async Task Decrypt(FileInfo file, FileInfo key, IProgress<long> progress, CancellationToken token)
        {
            RSA.RSAKeyPair? decryptKey = null;

            DecryptMessage = "키 불러오는 중...";
            await Task.Delay(500);
            using (var fs = key.OpenRead())
            {
                BinaryFormatter bf = new BinaryFormatter();
                decryptKey = bf.Deserialize(fs) as RSA.RSAKeyPair?;
            }

            string originalFileName;
            if (!ExtractFileName(file.Name, out originalFileName))
            {
                DecryptMessage = "파일 이름이 올바르지 않습니다.";
                await Task.Delay(500);
                return;
            }
            string originalExtension = ExtractExtension(originalFileName);


            if (decryptKey != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = originalFileName;
                if (!string.IsNullOrWhiteSpace(originalExtension))
                    sfd.Filter = $"Original File|*.{originalExtension}";

                if (sfd.ShowDialog() ?? false)
                {
                    Queue<byte> buffer = new Queue<byte>();
                    saveFilePath = sfd.FileName;
                    DecryptMessage = "키 불러오기 완료.";
                    await Task.Delay(500);
                    RSA.RSAKeyPair keyPair = decryptKey.Value;


                    State = RSA.EncryptionState.CreateMap;
                    MaxProgress = 255;
                    var map = await Decrypter.CreateByteMap(keyPair, progress);
                    var updateTick = EncryptViewModel.GetUpdateTick(EncryptedFileLength);

                    State = RSA.EncryptionState.ConvertByte;
                    MaxProgress = EncryptedFileLength;
                    await Task.Delay(500);

                    using (var inStream = file.OpenRead())
                    {
                        using (var reader = new BinaryReader(inStream))
                        {
                            using (var outStream = File.OpenWrite(saveFilePath))
                            {
                                using (var writer = new BinaryWriter(outStream))
                                {
                                    timeHelper.Reset(EncryptedFileLength);
                                    for (long i = 0; i < EncryptedFileLength; i++)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        byte b = reader.ReadByte();
                                        buffer.Enqueue(map[b]);
                                        if (i % updateTick == 0)
                                        {
                                            while (buffer.Count > 0)
                                                writer.Write(buffer.Dequeue());
                                            progress.Report(i);
                                            buffer.Clear();
                                            await Task.Delay(1);
                                        }
                                    }
                                    while (buffer.Count > 0)
                                        writer.Write(buffer.Dequeue());
                                    progress.Report(EncryptedFileLength);
                                }
                            }
                        }
                    }
                    DecryptMessage = "작업이 완료 되었습니다.";
                    var now = DateTime.Now;
                    Completed?.Invoke("작업 완료", $"완료 시간 : {now}\n걸린 시간 : {(now - timeHelper.StartTime).TotalSeconds} 초");
                }
                else
                {
                    DecryptMessage = "사용자가 작업을 취소했습니다.";
                }
            }
            else
            {
                DecryptMessage = "키가 잘못된 듯 합니다.";
            }
            await Task.Delay(500);
        }

        private long EncryptedFileLength { get; set; }
        private RSA.EncryptionState State { get; set; }
        private long maxProgress;
        public long MaxProgress { get { return maxProgress; } set { maxProgress = value; RaisePropertyChanged(nameof(MaxProgress)); } }
        private long currentProgress;
        public long CurrentProgress { get { return currentProgress; } set { currentProgress = value; RaisePropertyChanged(nameof(CurrentProgress)); } }

        private async Task<IProgress<long>> Set(long fileLength)
        {
            CurrentProgress = 0;
            EncryptedFileLength = fileLength;
            await Task.Delay(1);
            return new Progress<long>(Report);
        }

        private void Reset()
        {
            timeHelper = new Helper.ProgressTimeHelper();
            MaxProgress = 1;
            CurrentProgress = 0;
            EncryptedFilePath = KeyFilePath = null;
            DecryptMessage = "파일과 키를 선택해 주세요.";
            State = RSA.EncryptionState.None;
        }

        private void Report(long n)
        {
            if (State == RSA.EncryptionState.CreateMap)
                DecryptMessage = $"표 작성 중... ({n} / 256)";
            else if (State == RSA.EncryptionState.ConvertByte)
            {
                double d = timeHelper.Tick(n);
                var estimated = DateTime.Now.AddSeconds(d);
                DecryptMessage = $"변환 중... ({n} / {EncryptedFileLength}, 예상 완료 시간 : {estimated})";
            }
            CurrentProgress = n;
        }
        #endregion

        #region Regular Expression for File Name
        private Regex encryptExtensionRegex;
        private Regex fileExtensionRegex;

        private void SetRegularExpressions()
        {
            encryptExtensionRegex = new Regex(@"(.*)\.encrypted");
            fileExtensionRegex = new Regex(@".*\.([^.]*)");
        }

        private bool ExtractFileName(string encryptedFileName, out string originalFileName)
        {
            if (encryptExtensionRegex.IsMatch(encryptedFileName))
            {
                var catched = encryptExtensionRegex.Match(encryptedFileName);
                originalFileName = catched?.Groups?[1]?.Value;
                return true;
            }
            else
            {
                originalFileName = string.Empty;
                return false;
            }
        }

        private string ExtractExtension(string fileName)
        {
            return fileExtensionRegex.Match(fileName).Groups?[1]?.Value;
        }
        #endregion


        #region Constructor
        public DecryptViewModel()
        {
            SetRegularExpressions();
            IsIdle = true;
            Decrypter = RSA.RSAByteEncrypter.Encrypter;
            SelectFileCommand = new Commands.Decrypt.SelectFileCommand(s => EncryptedFilePath = s);
            SelectKeyCommand = new Commands.Decrypt.SelectFileCommand(s => KeyFilePath = s);
            CancelCommand = new Commands.Encrypt.CancelCommand(Cancel);
            DecryptCommand = new Commands.Decrypt.DecryptCommand(Decrypt, () => CanDecrypt);
            Reset();
        }
        #endregion

        #region Cancel
        public ICommand CancelCommand { get; }
        private CancellationTokenSource cancelTokenSource;
        private void Cancel()
        {
            try
            {
                cancelTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                DecryptMessage = $"취소 에러 : {e.Message}";
            }
        }
        #endregion
    }
}
