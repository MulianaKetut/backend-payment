using System;
namespace PaymentAPI.Models{
    public class PaymentDetail{
        public int PaymentDetailId { get; set;}
        public string CardOwnerName { get; set;}
        public string CardNumber { get; set;}
        public DateTime ExpirationDate { get; set;}
        public string SecurityCode { get; set;}

    }
}