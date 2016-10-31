using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernFileEncrypter.Commands.Decrypt
{
    public class DecryptCommand : RelayCommand
    {
        private Func<Task> decryptAction;
        private Func<bool> canDecrypt;

        public DecryptCommand(Func<Task> decrypt, Func<bool> canDecrypt)
        {
            decryptAction = decrypt;
            this.canDecrypt = canDecrypt;
        }

        public override bool CanExecute(object parameter)
        {
            return canDecrypt?.Invoke() ?? false;
        }

        public override async void Execute(object parameter)
        {
            await decryptAction?.Invoke();
            GC.Collect();
        }
    }
}
