﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class bdBusEntities : DbContext
    {
        public bdBusEntities()
            : base("name=bdBusEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<aBus> aBus { get; set; }
        public DbSet<Big_Location> Big_Location { get; set; }
        public DbSet<bz_day> bz_day { get; set; }
        public DbSet<bz_koeff> bz_koeff { get; set; }
        public DbSet<bz_week> bz_week { get; set; }
        public DbSet<bz_year> bz_year { get; set; }
        public DbSet<sBuses> sBuses { get; set; }
        public DbSet<sHolidays> sHolidays { get; set; }
        public DbSet<sKoord_point> sKoord_point { get; set; }
        public DbSet<sLocation> sLocation { get; set; }
        public DbSet<sLocation_Name> sLocation_Name { get; set; }
        public DbSet<sMarhList> sMarhList { get; set; }
        public DbSet<sMarshruts> sMarshruts { get; set; }
        public DbSet<sName_to_ost> sName_to_ost { get; set; }
        public DbSet<sOst_name> sOst_name { get; set; }
        public DbSet<sOstnovkis> sOstnovkis { get; set; }
        public DbSet<Statist> Statist { get; set; }
    }
}
