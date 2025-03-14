﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xarial.CadPlus.Plus.Exceptions;

namespace Xarial.CadPlus.Batch.StandAlone.Exceptions
{
    /// <summary>
    /// Represents the exception from VBA macro raised with Err.Raise
    /// </summary>
    public class VbaMacroException : UserException
    {
        public VbaMacroException(string error) : base(error)
        {
        }
    }
}
