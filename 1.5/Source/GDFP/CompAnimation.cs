﻿using Verse;

namespace GDFP;

public class CompAnimation : ThingComp
{
    public bool Reset = false;
    public CompProperties_Animation Props
    {
        get
        {
            return (CompProperties_Animation)this.props;
        }
    }
}
