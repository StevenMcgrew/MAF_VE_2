using SQLite.Net.Attributes;

namespace MAF_VE_2.Models
{
    class LocalCarMake
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        public string Make { get; set; }
    }
}
