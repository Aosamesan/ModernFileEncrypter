using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernFileEncrypter.Commands.Encrypt
{
    public class CancelCommand : RelayCommand
    {
        private Action cancelAction;

        public CancelCommand(Action cancel)
        {
            cancelAction = cancel;
        }

        public override bool CanExecute(object parameter)
        {
            return !((parameter as bool?) ?? true);
        }

        public override void Execute(object parameter)
        {
            cancelAction?.Invoke();
        }
    }
}
