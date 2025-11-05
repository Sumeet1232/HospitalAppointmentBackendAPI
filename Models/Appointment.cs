namespace HospitalAppointmentApi.Models
{
    public class Appointment
    {
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Gender { get; set; }
        public DateTime AppointmentTime { get; set; }
        public string Problem { get; set; }
        public string Status { get; set; } = "Waiting";
    }

}
