using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModernFileEncrypter.Commands.Encrypt
{
    public class SelectFolderCommand : RelayCommand
    {
        private Action<string> pathSetter;

        public SelectFolderCommand(Action<string> pathSetter)
        {
            this.pathSetter = pathSetter;
        }

        public override bool CanExecute(object parameter) => true;

        public override void Execute(object parameter)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if(fbd.ShowDialog() == DialogResult.OK)
                {
                    pathSetter.Invoke(fbd.SelectedPath);
                }
            }
        }
    }
}
