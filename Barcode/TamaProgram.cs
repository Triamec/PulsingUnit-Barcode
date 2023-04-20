// The number of these two  directives must match the device's Tama Virtual Machine ID and Register Layout ID,
// respectively.
using System.Reflection;
using Triamec.Tama.Rlid19;
using Triamec.Tama.Vmid5;
using Triamec.TriaLink;

[assembly: AssemblyVersion("2.0.0.0")]

// The class name (as well as the project name and the namespace) contributes to the file name of the produced Tama
// program. This file is located in the bin\Debug or bin\Release subfolders and will commonly be copied into the
// Tama directory of the default workspace, too.
[Tama]
internal static class TamaProgram
{

    // Linear State Machine
    private enum State
    {
        Idle,
        MoveToStart,
        FillFifoPositive,
        MoveRowPositive,
        FillFifoNegative,
        MoveRowNegative
    }

    private static class Command
    {
        public const int None = 0;
        public const int Go = 1;
    }

    // Static variables can be used, but are not shared between the AsynchronousMain and the other tasks (Imagine a
    // [ThreadStatic] attribute here). For sharing, use general purpose registers.
    static int _rows;
    static int _row_index;
    static readonly uint[] cPulseCountPositive = { 8, 7, 8, 7, 5};
    static readonly uint[] cPulseCountNegative = { 8, 7, 8, 7, 5};
    static readonly int[] cPulseOn = { 1, 0, 1, 0, 1 };

    const float cReferencePositive = 20f;
    const float cReferenceNegative = 23.3f;
    const float cDeltaPosition = 0.1f;
    const float cPulseWidth = 0.00001f;

    // Choose how to run the program. Additional entry points for other tasks can be specified in this same program.
    [TamaTask(Task.IsochronousMain)]
    static void Main()
    {

        // Template state machine showing the picture of how a task is structured and which registers are commonly used
        // for status/control operations.
        switch ((State) Register.Application.TamaControl.IsochronousMainState)
        {
            case State.Idle:
                switch (Register.Application.TamaControl.IsochronousMainCommand)
                {
                    case Command.Go:
                        _rows = Register.Application.Parameters.Integers[0];
                        _row_index = 0;
                        MoveTo(0);
                        Register.Application.TamaControl.IsochronousMainState = (int)State.MoveToStart;
                        break;
                }
                break;
            case State.MoveToStart:
                if (Register.Axes_0.Signals.PathPlanner.Done)
                {
                    if (_rows > 0)
                    {
                        Register.Axes_0.Commands.OptionModule.PU_Mode = OptionPuMode.Disabled;
                        Register.Application.TamaControl.IsochronousMainState = (int)State.FillFifoPositive;
                    } else
                    {
                        Register.Application.TamaControl.IsochronousMainState = (int)State.Idle;
                    }
                }
                break;
            case State.FillFifoPositive:
                if (_row_index < _rows)
                {
                    uint segment_index = Register.Axes_0.Signals.OptionModule.PU_ActualPulseCount;
                    if (segment_index < cPulseCountPositive.Length)
                    {
                        Register.Axes_0.Commands.OptionModule.PU_ReferencePosition = cReferencePositive;
                        Register.Axes_0.Commands.OptionModule.PU_DeltaPosition = cDeltaPosition;
                        Register.Axes_0.Commands.OptionModule.PU_PulseWidth = cPulseWidth * cPulseOn[segment_index];
                        Register.Axes_0.Commands.OptionModule.PU_Count = cPulseCountPositive[segment_index];
                        Register.Axes_0.Commands.OptionModule.PU_Fifo = OptionPuFifo.Append;
                    }
                    else
                    {
                        MoveTo(24);
                        Register.Application.TamaControl.IsochronousMainState = (int)State.MoveRowPositive;
                    }
                }
                else
                {
                    Register.Application.TamaControl.IsochronousMainState = (int)State.Idle;
                }
                break;
            case State.MoveRowPositive:
                if (Register.Axes_0.Signals.PathPlanner.Done)
                {
                    if (Register.Axes_0.Signals.OptionModule.PU_ActualPulseCount != 0)
                    {
                        // abort and indicate an error with Booleans[0]
                        Register.Application.TamaControl.IsochronousMainState = (int)State.Idle;
                        Register.Application.Variables.Booleans[0] = true;
                        break;
                    }
                    _row_index++;
                    Register.Application.TamaControl.IsochronousMainState = (int)State.FillFifoNegative;
                    // Move other Axis
                }
                break;
            case State.FillFifoNegative:
                if (_row_index < _rows)
                {
                    uint segment_index = Register.Axes_0.Signals.OptionModule.PU_ActualPulseCount;
                    if (segment_index < cPulseCountPositive.Length)
                    {
                        Register.Axes_0.Commands.OptionModule.PU_ReferencePosition = cReferenceNegative;
                        Register.Axes_0.Commands.OptionModule.PU_DeltaPosition = -cDeltaPosition;
                        Register.Axes_0.Commands.OptionModule.PU_PulseWidth = cPulseWidth * cPulseOn[segment_index];
                        Register.Axes_0.Commands.OptionModule.PU_Count = cPulseCountNegative[segment_index];
                        Register.Axes_0.Commands.OptionModule.PU_Fifo = OptionPuFifo.Append;
                    }
                    else
                    {
                        MoveTo(20);
                        Register.Application.TamaControl.IsochronousMainState = (int)State.MoveRowNegative;
                        _row_index++;
                    }
                }
                else
                {
                    Register.Application.TamaControl.IsochronousMainState = (int)State.Idle;
                }
                break;
            case State.MoveRowNegative:
                if (Register.Axes_0.Signals.PathPlanner.Done)
                {
                    if (Register.Axes_0.Signals.OptionModule.PU_ActualPulseCount != 0)
                    {
                        // abort and indicate an error with Booleans[0]
                        Register.Application.TamaControl.IsochronousMainState = (int)State.Idle;
                        Register.Application.Variables.Booleans[0] = true;
                        break;
                    }
                    _row_index++;
                    Register.Application.TamaControl.IsochronousMainState = (int)State.FillFifoPositive;
                    // Move other Axis
                }
                break;

        }
    }

    static void MoveTo(float position)
    {
        Register.Axes_0.Commands.PathPlanner.Xnew = position;
        Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute;
    }
}
