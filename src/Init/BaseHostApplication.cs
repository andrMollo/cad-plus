﻿using System;
using System.Collections.Generic;
using Xarial.CadPlus.Plus;
using Xarial.XCad;

namespace Xarial.CadPlus.Module.Init
{
    public abstract class BaseHostApplication : IHostApplication
    {
        public abstract IntPtr ParentWindow { get; }
        public abstract IEnumerable<IModule> Modules { get; }
        public virtual IServiceProvider Services { get; }

        public abstract event Action Connect;
        public abstract event Action Disconnect;
        
        public virtual void OnStarted() 
        { 
        }
        
        public virtual void OnConfigureServices(IXServiceCollection svcColl) 
        {
        }
        
        public virtual void Dispose() 
        {
        }
    }
}
