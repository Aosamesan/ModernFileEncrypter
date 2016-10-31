using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernFileEncrypter.Commands.Encrypt
{
    public class EncryptCommand : RelayCommand
    {
        private Func<FileInfo, Task> encryptAction;
        private Func<bool> isIdle;

        public EncryptCommand(Func<FileInfo, Task> encrypt, Func<bool> isIdle)
        {
            encryptAction = encrypt;
            this.isIdle = isIdle;
        }

        public override bool CanExecute(object parameter)
        {
            return parameter is FileInfo && isIdle.Invoke();
        }

        public override async void Execute(object parameter)
        {
            await encryptAction?.Invoke(parameter as FileInfo);
            GC.Collect();
        }
    }
}
