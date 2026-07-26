using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JlptTrainer.Application.Common.Exceptions
{
    public sealed class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException() : base("Bạn không có quyền thao tác trên tài nguyên này.") { }
    }
}
