using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernFileEncrypter.Commands.Decrypt
{
    public class SelectFileCommand : RelayCommand
    {
        private Action<string> pathSetter;

        public SelectFileCommand(Action<string> setter)
        {
            pathSetter = setter;
        }

        public override bool CanExecute(object parameter) => !string.IsNullOrWhiteSpace(parameter as string);

        public override void Execute(object parameter)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = parameter as string;

            if(ofd.ShowDialog() ?? false)
            {
                pathSetter?.Invoke(ofd.FileName);
            }
        }
    }
}
