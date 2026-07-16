using Monitor = Lopingo.Data.Entities.Monitor;
using Incident = Lopingo.Data.Entities.Incident;

namespace Lopingo.Core.Buses;

public sealed record MonitorUpdated(Monitor Monitor, Incident? Incident, bool TransitionedToDown);
