using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Lge.Tools.Download
{
    public class DelegateCommand : ICommand
    {
        public DelegateCommand()
                       : this(null, null)
        {
        }

        public DelegateCommand(Action<object> execute)
                       : this(execute, null)
        {
        }

        public DelegateCommand(Action<object> execute,
                       Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            if (_execute != null)
                _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                Extension.UIThread(delegate
                {
                    CanExecuteChanged(this, EventArgs.Empty);
                });
            }
        }

        private Predicate<object> _canExecute;
        private Action<object> _execute;

        public event EventHandler CanExecuteChanged;
        public Predicate<object> CanExecuteHandler
        {
            get { return _canExecute; }
            set { _canExecute = value; }
        }

        public Action<object> ExecuteHandler
        {
            get { return _execute; }
            set { _execute = value; }
        }
    }
}
