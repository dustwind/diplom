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
    
    public partial class Big_Location
    {
        public Big_Location()
        {
            this.sLocation = new HashSet<sLocation>();
        }
    
        public int Id_big { get; set; }
        public int id_koord1 { get; set; }
        public int id_koord2 { get; set; }
        public double length { get; set; }
    
        public virtual ICollection<sLocation> sLocation { get; set; }
        public virtual sKoord_point sKoord_point { get; set; }
        public virtual sKoord_point sKoord_point1 { get; set; }
    }
}
