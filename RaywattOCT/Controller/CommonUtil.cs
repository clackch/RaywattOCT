using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RaywattOCT.Controller
{
    public class CommonUtil
    {
        public static bool ValidateInput(string input)
        {
            var regex = new Regex(@"^[a-zA-Z0-9ㄱ-ㅎ가-힣\s,.]+$");

            if (input.Length == 0) return true;

            return regex.IsMatch(input);
        }
    }
}
