namespace ProjectTest1.ViewModels
{
    public class PaymentResultViewModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public PaymentResultViewModel() { }
        public PaymentResultViewModel(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }
    }
}
