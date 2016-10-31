using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ModernFileEncrypter.ViewModels
{
    public delegate void ShowMessageBox(string caption, string message);

    public class EncryptViewModel : NotifyPropertyChanged
    {
        #region Folder
        public string SelectedFolderPath { get; set; }
        public IEnumerable<FileInfo> Files { get; set; }
        public ICommand SelectFolderCommand { get; }
        public ICommand RefreshFolderCommand { get; }

        private void SetFolderPath(string path)
        {
            SelectedFolderPath = path;
            Files = Array.ConvertAll(Directory.GetFiles(SelectedFolderPath), (filePath) => new FileInfo(filePath));
            RaisePropertyChanged(nameof(SelectedFolderPath));
            RaisePropertyChanged(nameof(Files));
        }
        #endregion

        #region Show Message Box
        public event ShowMessageBox Completed;
        #endregion

        #region Idle
        private bool isIdle;
        public bool IsIdle
        {
            get
            {
                return isIdle;
            }
            set
            {
                isIdle = value;
                RaisePropertyChanged(nameof(IsIdle), nameof(IsRunning));
            }
        }
        public bool IsRunning
        {
            get
            {
                return !IsIdle;
            }
        }
        #endregion

        #region Encrypt Status
        private string encryptMessage;
        public string EncryptMessage
        {
            get
            {
                return encryptMessage;
            }
            set
            {
                encryptMessage = value;
                RaisePropertyChanged(nameof(EncryptMessage));
            }
        }

        private FileInfo selectedFile;
        public FileInfo SelectedFile
        {
            get
            {
                return selectedFile;
            }
            set
            {
                selectedFile = value;
                EncryptMessage = $"선택된 파일 : {SelectedFile?.Name}";
                RaisePropertyChanged(nameof(SelectedFile));
                RaisePropertyChanged(nameof(IsFileSelected));
            }
        }

        public bool IsFileSelected
        {
            get
            {
                return SelectedFile != null;
            }
        }

        private long currentProgress;
        public long CurrentProgress
        {
            get { return currentProgress; }
            set
            {
                currentProgress = value;
                RaisePropertyChanged(nameof(CurrentProgress));
            }
        }

        private long maxProgress;
        public long MaxProgress
        {
            get { return maxProgress; }
            set
            {
                maxProgress = value;
                RaisePropertyChanged(nameof(MaxProgress));
            }
        }

        private long SelectedFileLength { get; set; }
        private RSA.EncryptionState State { get; set; }

        Helper.ProgressTimeHelper timeHelper;

        private void Reset()
        {
            SelectedFile = null;
            MaxProgress = 1;
            CurrentProgress = 0;
            EncryptMessage = "파일을 선택해 주세요.";
            State = RSA.EncryptionState.None;
        }

        private async Task<IProgress<long>> Set(long fileLength)
        {
            CurrentProgress = 0;
            SelectedFileLength = fileLength;
            await Task.Delay(1);
            return new Progress<long>(Report);
        }

        private void Report(long n)
        {
            if (State == RSA.EncryptionState.CreateMap)
                EncryptMessage = $"표 작성 중... ({n} / 256)";
            else if (State == RSA.EncryptionState.ConvertByte)
            {
                double d = timeHelper.Tick(n);
                var estimated = DateTime.Now.AddSeconds(d);
                EncryptMessage = $"변환 중... ({n} / {SelectedFileLength}, 예상 완료 시간 : {estimated})";
            }
            CurrentProgress = n;
        }

        public ICommand EncryptCommand { get; }
        CancellationTokenSource cancelTokenSource;
        public ICommand CancelTaskCommand { get; }

        private void Cancel()
        {
            try
            {
                cancelTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                EncryptMessage = $"Cancel Error : {e.Message}";
            }
        }

        private async Task Encrypt(FileInfo file)
        {
            var progress = await Set(file.Length);
            string encryptedFilePath = $"{file.FullName}.encrypted";
            string decryptKeyFilePath = $"{file.FullName}.key";
            using (cancelTokenSource = new CancellationTokenSource())
            {
                IsIdle = false;
                try
                {
                    await Encrypt(file, progress, cancelTokenSource.Token);
                    Reset();
                }
                catch (OperationCanceledException e)
                {
                    EncryptMessage = "작업이 취소되었습니다.";
                    await Task.Delay(1);
                    if (File.Exists(encryptedFilePath))
                        File.Delete(encryptedFilePath);
                    if (File.Exists(decryptKeyFilePath))
                        File.Delete(decryptKeyFilePath);
                }
                IsIdle = true;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async void RefreshCurrentFolder()
        {
            SetFolderPath(SelectedFolderPath);
            await Task.Delay(1);
        }

        private async Task Encrypt(FileInfo file, IProgress<long> progress, CancellationToken token)
        {
            Random r = new Random();
            int seed = r.Next(350000) + 258;
            long updateTick = GetUpdateTick(file.Length);
            Queue<byte> buffer = new Queue<byte>();
            
            
            EncryptMessage = "키 생성 중...";
            await Task.Delay(1);
            var keys = await ByteEncrypter.GenerateKeyPairs(seed);
            var encryptKey = keys[0];
            var decryptKey = keys[1];
            EncryptMessage = "키 생성 완료..";
            await Task.Delay(500);

            State = RSA.EncryptionState.CreateMap;
            MaxProgress = 255;
            var map = await ByteEncrypter.CreateByteMap(encryptKey, progress);

            string encryptedFilePath = $"{file.FullName}.encrypted";
            string decryptKeyFilePath = $"{file.FullName}.key";

            State = RSA.EncryptionState.ConvertByte;
            MaxProgress = SelectedFileLength;
            await Task.Delay(500);
            using (var inStream = file.OpenRead())
            {
                using (var reader = new BinaryReader(inStream))
                {
                    using (var outStream = File.OpenWrite(encryptedFilePath))
                    {
                        using (var writer = new BinaryWriter(outStream))
                        {
                            timeHelper.Reset(SelectedFileLength);
                            for (long i = 0; i < SelectedFileLength; i++)
                            {
                                token.ThrowIfCancellationRequested();
                                byte b = reader.ReadByte();
                                buffer.Enqueue(map[b]);
                                if (i % updateTick == 0)
                                {
                                    while (buffer.Count > 0)
                                        writer.Write(buffer.Dequeue());
                                    buffer.Clear();
                                    progress.Report(i);
                                    await Task.Delay(1);
                                }
                            }
                            while (buffer.Count > 0)
                                writer.Write(buffer.Dequeue());
                            progress.Report(SelectedFileLength);
                        }
                    }
                }
            }
            await Task.Delay(500);
           
            EncryptMessage = "키 파일을 저장중입니다.";
            await Task.Delay(500);
            using (var fs = File.OpenWrite(decryptKeyFilePath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, decryptKey);
            }

            var now = DateTime.Now;
            Completed?.Invoke("작업 완료", $"완료 시간 : {now}\n걸린 시간 : {(now - timeHelper.StartTime).TotalSeconds} 초");
            RefreshCurrentFolder();
        }

        public static long GetUpdateTick(long length)
        {
            if (length < 1024) // 1KB 미만
                return 16;  // 16 bytes
            else if (length < 1048576) // 1MB 미만
                return 4096;    // 1 KB
            else if (length < 1048576 * 1024)   // 1GB 이하
                return 1048576 / 8;    // 128 KB
            else
                return 1048576 / 4;   // 256 KB
        }
        #endregion

        #region Encrypter
        private RSA.RSAByteEncrypter ByteEncrypter { get; }
        #endregion

        #region Constructor
        public EncryptViewModel()
        {
            timeHelper = new Helper.ProgressTimeHelper();
            ByteEncrypter = RSA.RSAByteEncrypter.Encrypter;
            IsIdle = true;
            SelectFolderCommand = new Commands.Encrypt.SelectFolderCommand(SetFolderPath);
            RefreshFolderCommand = new Commands.Encrypt.RefreshFolderListCommand(RefreshCurrentFolder);
            EncryptCommand = new Commands.Encrypt.EncryptCommand(Encrypt, () => IsIdle);
            CancelTaskCommand = new Commands.Encrypt.CancelCommand(Cancel);
            Reset();
        }
        #endregion
    }
}
