using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BrokenRailServer.Classes
{
    class ValidationRules
    {
    }

    public class ValidationRuleFrom0To255 : ValidationRule
    {
        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult result = new ValidationResult(true, null);
            string inputString = (value ?? string.Empty).ToString();

            int parseResult = -1;
            if (string.IsNullOrEmpty(inputString)
                || !int.TryParse(inputString, out parseResult)
                || parseResult < 0
                || parseResult > 255)
            {
                result = new ValidationResult(false, this.ErrorMessage);
            }
            return result;
        }
    }
    public class ValidationRuleFrom0To4095 : ValidationRule
    {
        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            ValidationResult result = new ValidationResult(true, null);
            string inputString = (value ?? string.Empty).ToString();

            int parseResult = -1;
            if (string.IsNullOrEmpty(inputString)
                || !int.TryParse(inputString, out parseResult)
                || parseResult < 0
                || parseResult > 4095)
            {
                result = new ValidationResult(false, this.ErrorMessage);
            }
            return result;
        }
    }
}
