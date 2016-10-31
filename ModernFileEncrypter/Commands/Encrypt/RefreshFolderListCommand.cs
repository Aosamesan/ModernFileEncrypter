using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernFileEncrypter.Commands.Encrypt
{
    public class RefreshFolderListCommand : RelayCommand
    {
        private Action refresh;

        public RefreshFolderListCommand(Action refresh)
        {
            this.refresh = refresh;
        }

        public override bool CanExecute(object parameter)
            => Directory.Exists(parameter as string);

        public override void Execute(object parameter)
        {
            refresh?.Invoke();
        }
    }
}
