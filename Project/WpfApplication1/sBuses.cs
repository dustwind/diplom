//------------------------------------------------------------------------------
// <auto-generated>
//    Этот код был создан из шаблона.
//
//    Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//    Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WpfApplication1
{
    using System;
    using System.Collections.Generic;
    
    public partial class sBuses
    {
        public int id { get; set; }
        public Nullable<int> id_marsh { get; set; }
        public int nom { get; set; }
        public string marka_ts { get; set; }
    
        public virtual sMarshruts sMarshruts { get; set; }
    }
}