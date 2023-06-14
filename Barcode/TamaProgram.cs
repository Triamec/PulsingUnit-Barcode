using Triamec.Tama.Rlid19;
using Triamec.Tama.Vmid5;
using Triamec.TriaLink;

// The class name (as well as the project name and the namespace) contributes to the file name of the produced Tama
// program. This file is located in the bin\Debug or bin\Release subfolders and will commonly be copied into the
// Tama directory of the default workspace, too.
[Tama]
internal class TamaProgram
{

    // Linear State Machine, shown in Register.Application.TamaControl.IsochronousMainState
    private enum State
    {
        Idle,
        MoveToStart,
        FillFifoPositive,
        MoveRowPositive,
        FillFifoNegative,
        MoveRowNegative
    }

    // User commands, invoked at Register.Application.TamaControl.IsochronousMainCommand
    private static class Command
    {
        public const int None = 0;
        public const int Go = 1;
    }

    // pulsing application constants
    const int cRows = 2;
    const float cMoveStartPosition = 19f;
    const float cMoveEndPosition = 24.3f;
    const float cReferencePositive = 20f;
    const float cReferenceNegative = 23.3f;
    const float cDeltaPosition = 0.1f;
    const float cPulseWidth = 0.00001f;

    // internal variables
    static int _row_index;
    static int[] cPulseCountPositive = new int[5];
    static int[] cPulseCountNegative = new int[5];
    static int[] cPulseOn = new int[5];

    // Constructor
    public TamaProgram()
    {
        cPulseCountPositive[0] = 8;
        cPulseCountPositive[1] = 5;
        cPulseCountPositive[2] = 8;
        cPulseCountPositive[3] = 5;
        cPulseCountPositive[4] = 5;

        cPulseCountNegative[0] = 5;
        cPulseCountNegative[1] = 5;
        cPulseCountNegative[2] = 8;
        cPulseCountNegative[3] = 5;
        cPulseCountNegative[4] = 8;

        cPulseOn[0] = 1;
        cPulseOn[1] = 0;
        cPulseOn[2] = 1;
        cPulseOn[3] = 0;
        cPulseOn[4] = 1;
    }

    // -- entry point --
    [TamaTask(Task.IsochronousMain)]
    static void Main()
    {
        switch ((State)Register.Application.TamaControl.IsochronousMainState)
        {
            case State.Idle:
                switch (Register.Application.TamaControl.IsochronousMainCommand)
                {
                    case Command.Go:
                        _row_index = 0;
                        MoveTo(cMoveStartPosition);
                        Register.Application.TamaControl.IsochronousMainState = (int)State.MoveToStart;
                        break;
                }
                break;
            case State.MoveToStart:
                if (Register.Axes_0.Signals.PathPlanner.Done)
                {
                    Register.Axes_0.Commands.OptionModule.PU_Mode = OptionPuMode.Disabled;
                    Register.Application.TamaControl.IsochronousMainState = (int)State.FillFifoPositive;
                }
                break;
            case State.FillFifoPositive:
                // set row reference position before activating the mode
                if (Register.Axes_0.Commands.OptionModule.PU_ReferencePosition != cReferencePositive)
                {
                    Register.Axes_0.Commands.OptionModule.PU_ReferencePosition = cReferencePositive;
                    break;
                }
                if (Register.Axes_0.Commands.OptionModule.PU_Mode != OptionPuMode.Fifo)
                {
                    Register.Axes_0.Commands.OptionModule.PU_Mode = OptionPuMode.Fifo;
                    break;
                }
                if (_row_index < cRows)
                {
                    int segment_index = 512 - (int)Register.Axes_0.Signals.OptionModule.PU_FreeFifoEntries;
                    if (segment_index < cPulseCountPositive.Length)
                    {
                        Register.Axes_0.Commands.OptionModule.PU_DeltaPosition = cDeltaPosition;
                        Register.Axes_0.Commands.OptionModule.PU_PulseWidth = cPulseWidth * cPulseOn[segment_index];
                        Register.Axes_0.Commands.OptionModule.PU_Count = (uint) cPulseCountPositive[segment_index];
                        Register.Axes_0.Commands.OptionModule.PU_Fifo = OptionPuFifo.Append;
                    }
                    else
                    {
                        MoveTo(cMoveEndPosition);
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
                    _row_index++;
                    Register.Axes_0.Commands.OptionModule.PU_Mode = OptionPuMode.Disabled;
                    Register.Application.TamaControl.IsochronousMainState = (int)State.FillFifoNegative;

                    // -- Move other Axis to align with next row --
                }
                break;
            case State.FillFifoNegative:
                // set row reference position before activating the mode
                if (Register.Axes_0.Commands.OptionModule.PU_ReferencePosition != cReferencePositive)
                {
                    Register.Axes_0.Commands.OptionModule.PU_ReferencePosition = cReferenceNegative;
                    break;
                }
                if (Register.Axes_0.Commands.OptionModule.PU_Mode != OptionPuMode.Fifo)
                {
                    Register.Axes_0.Commands.OptionModule.PU_Mode = OptionPuMode.Fifo;
                    break;
                }
                if (_row_index < cRows)
                {
                    int segment_index = 512 - (int)Register.Axes_0.Signals.OptionModule.PU_FreeFifoEntries;
                    if (segment_index < cPulseCountPositive.Length)
                    {
                        Register.Axes_0.Commands.OptionModule.PU_DeltaPosition = -cDeltaPosition;
                        Register.Axes_0.Commands.OptionModule.PU_PulseWidth = cPulseWidth * cPulseOn[segment_index];
                        Register.Axes_0.Commands.OptionModule.PU_Count = (uint) cPulseCountNegative[segment_index];
                        Register.Axes_0.Commands.OptionModule.PU_Fifo = OptionPuFifo.Append;
                    }
                    else
                    {
                        MoveTo(cMoveStartPosition);
                        Register.Application.TamaControl.IsochronousMainState = (int)State.MoveRowNegative;
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
                    _row_index++;
                    Register.Axes_0.Commands.OptionModule.PU_Mode = OptionPuMode.Disabled;
                    Register.Application.TamaControl.IsochronousMainState = (int)State.FillFifoPositive;

                    // -- Move other Axis to align with next row --
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
