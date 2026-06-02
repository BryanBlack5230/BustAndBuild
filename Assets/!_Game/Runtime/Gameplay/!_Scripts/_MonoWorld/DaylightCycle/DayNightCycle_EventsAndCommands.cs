using BarkingBird.Runtime.Infrastructure;
using BarkingBird.Runtime.Infrastructure.Commands;

namespace BarkingBird.Runtime.Gameplay.Daylight
{
    // Commands
    public readonly struct StartDayCommand : ICommand { }
    public readonly struct ForceFinishDayCommand : ICommand { }

    // Events
    public readonly struct DayStartedEvent : IEvent { }
    public readonly struct DayEndedEvent : IEvent { }
}
