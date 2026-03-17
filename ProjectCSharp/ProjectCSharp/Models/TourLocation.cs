namespace ProjectCSharp.Models
{
    public class TourLocation
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string ImageUrl { get; set; }
        public required string AudioFileName { get; set; }

        // Tạm thời cứ khai báo tọa độ để sẵn sàng cho lúc có API Maps
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
