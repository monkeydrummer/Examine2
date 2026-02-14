namespace CAD2DModel.Rendering;

/// <summary>
/// Static utility class for calculating "nice" interval spacing for ruler ticks.
/// Ported from C++ CWSAxis::Nice_Interval2 algorithm (Brent Corkum).
/// </summary>
public static class NiceInterval
{
    /// <summary>
    /// Calculates optimal min, max, and delta values for ruler tick spacing.
    /// This algorithm finds "nice" increments (1.0, 2.0, 2.5, 5.0 scaled by powers of 10)
    /// that fit the data range with the requested number of intervals.
    /// </summary>
    /// <param name="minValue">Input: minimum value of range. Output: adjusted minimum aligned to tick boundary.</param>
    /// <param name="maxValue">Input: maximum value of range. Output: adjusted maximum aligned to tick boundary.</param>
    /// <param name="delta">Output: spacing between major ticks.</param>
    /// <param name="numIntervals">Desired number of intervals (major ticks).</param>
    public static void CalculateNiceInterval(ref double minValue, ref double maxValue, out double delta, int numIntervals)
    {
        const double relativeEpsilon = 1e-9;
        double epsilon = relativeEpsilon * (Math.Abs(maxValue) + Math.Abs(minValue));
        if (epsilon < relativeEpsilon)
            epsilon = relativeEpsilon;

        // Calculate base delta
        double dv = (maxValue - minValue) / numIntervals;
        
        // Handle edge case: if range is too small, expand it slightly
        if (Math.Abs(dv) < epsilon)
        {
            maxValue += epsilon;
            minValue -= epsilon;
            dv = (maxValue - minValue) / numIntervals;
            delta = dv;
            return;
        }

        // Find power of 10 scale factor
        double nm;
        if (dv < 1.0)
            nm = Math.Pow(10.0, (int)(Math.Log10(dv) - 0.99999));
        else
            nm = Math.Pow(10.0, (int)Math.Log10(dv));

        // Test increments: 1.0, 2.0, 2.5, 5.0 scaled by powers of 10
        const int numSteps = 4;
        double[] inc = { 1.0, 2.0, 2.5, 5.0 };
        double[] ainc = new double[numSteps];
        
        bool found = false;
        int i = 0;
        
        // Try scaling factors (nm, nm*10)
        while (!found && i <= 1)
        {
            int j = 0;
            while (!found && j < numSteps)
            {
                double dvTry = inc[j] * nm * Math.Pow(10.0, i);
                
                if (dvTry >= dv)
                {
                    // Found a suitable base increment, now calculate the array of increments
                    for (int k = 0; k < numSteps; k++)
                    {
                        int idx = (j + k) % numSteps;
                        int powerAdjust = (j + k) / numSteps;
                        
                        if ((j + k) >= numSteps)
                            ainc[k] = inc[idx] * nm * Math.Pow(10.0, i + 1);
                        else
                            ainc[k] = inc[idx] * nm * Math.Pow(10.0, i);
                        
                        found = true;
                    }
                }
                j++;
            }
            i++;
        }

        // Select the smallest increment that fits all intervals
        for (int k = 0; k < numSteps; k++)
        {
            double start;
            if (minValue > 0.0)
                start = ainc[k] * (int)(minValue / ainc[k]);
            else
                start = ainc[k] * (int)((minValue / ainc[k]) - 0.9999999);
            
            double end = start + numIntervals * ainc[k];
            
            if (end > maxValue)
            {
                minValue = start;
                maxValue = end;
                delta = ainc[k];
                return;
            }
        }
        
        // Fallback (shouldn't reach here normally)
        delta = dv;
    }
}
