using System;

namespace Step.Utilities
{
    internal interface IPiecewiseLinearFunction
    {
        float DomainMin => ControlPointArgument(0);
        float DomainMax => ControlPointArgument(ControlPointCount - 1);
        int ControlPointCount { get; }
        float ControlPointArgument(int cpIndex);
        float ControlPointValue(int cpIndex);

        float Evaluate(float argument)
        {
            var afterPoint = ControlPointCount - 1;
            for (var i = 0; i < ControlPointCount; i++)
            {
                if (argument <= ControlPointArgument(i))
                {
                    afterPoint = i;
                    break;
                }
            }

            var beforePoint = Math.Max(0, afterPoint - 1);
            var startArgument = ControlPointArgument(beforePoint);
            var segmentWidth = ControlPointArgument(afterPoint) - startArgument;
            var segmentOffset = argument - startArgument;
            var control = Math.Max(0, Math.Min(1, segmentOffset / segmentWidth));
            return (1 - control) * ControlPointValue(beforePoint) + control * ControlPointValue(afterPoint);
        }
    }
}
