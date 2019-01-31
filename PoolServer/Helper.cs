using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prizmer.PoolServer
{
    public class Helper
    {
        public string GetEnumKeyAsString(Type enumType, object value)
        {
            string res = value.ToString();
            try
            {
                res = Enum.GetName(enumType, value);
            }
            catch (Exception ex)
            {
                //
            }

            return res;
        }

    }
}
