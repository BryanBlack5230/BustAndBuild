using System;

public class InputManager: IDisposable
{
    private readonly InputActions _inputActions;
    public InputActions Actions => _inputActions;

    public InputManager()
    {
        _inputActions = new InputActions();
        _inputActions.Enable();
    }

    public void Dispose()
    {
        _inputActions.Disable();
        _inputActions?.Dispose();
    }
}