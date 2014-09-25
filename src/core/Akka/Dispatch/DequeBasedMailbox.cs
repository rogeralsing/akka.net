﻿using Akka.Actor;

namespace Akka.Dispatch
{
    public interface DequeBasedMailbox
    {
        void EnqueueFirst(Envelope envelope);

        void Post(Envelope envelope);
    }
}
