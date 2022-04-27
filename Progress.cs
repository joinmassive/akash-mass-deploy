using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace akash_dep
{
    // Simple class to output progress
    public class ProgressConsole
    {
        readonly int m_maxElements;
        int m_cur;
        readonly String m_display;

        const String percentFormat = "0.#";

        public ProgressConsole(int maxElements, String display)
        {
            m_maxElements = maxElements;
            m_cur = 0;
            m_display = display;
        }

        public void Increment()
        {
            double curPercent = m_cur / (double)m_maxElements;
            String percent = (curPercent * 100.0).ToString(percentFormat);
            Console.WriteLine(m_display + " " + percent + "%");

            m_cur++;
        }

        public void SetPos(int pos)
        {
            m_cur = pos;
        }
    }
}
