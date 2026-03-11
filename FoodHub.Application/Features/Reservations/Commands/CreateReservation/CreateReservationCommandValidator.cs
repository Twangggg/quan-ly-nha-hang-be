using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Reservations.Commands.CreateReservation
{
    public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
    {
        public CreateReservationCommandValidator()
        {
            RuleFor(x => x.CustomerName)
                .NotEmpty().WithMessage("Tên khách hàng là bắt buộc.")
                .MaximumLength(100).WithMessage("Tên khách hàng không quá 100 kí tự.");

            RuleFor(x => x.CustomerPhone)
                .NotEmpty().WithMessage("Số điện thoại là bắt buộc.")
                .MaximumLength(20).WithMessage("Số điện thoại không hợp lệ.");

            RuleFor(x => x.GuestCount)
                .GreaterThan(0).WithMessage("Số lượng khách phải lớn hơn 0.");

            RuleFor(x => x.TableId)
                .NotEmpty().WithMessage("Vui lòng chọn bàn.");

            // AC-PR-01 & AC-PR-02: Validate time constraints
            RuleFor(x => x)
                .Must(x => IsValidReservationTime(x.ReservationDate, x.ReservationTime))
                .WithMessage("Thời gian nhận bàn không hợp lệ. Không được đặt ngày quá khứ. Nếu đặt hôm nay phải cách hiện tại ít nhất 45 phút.");
        }

        private bool IsValidReservationTime(DateOnly date, TimeSpan time)
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            
            if (date < today) return false;

            if (date == today)
            {
                var minTime = now.TimeOfDay.Add(TimeSpan.FromMinutes(45));
                if (time < minTime) return false;
            }

            return true;
        }
    }
}
