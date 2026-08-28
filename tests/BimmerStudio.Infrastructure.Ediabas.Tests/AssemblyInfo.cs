using Xunit;

// EdiabasNet keeps process-wide static state: SharedDataDict (an SGBD cache cleared whenever the
// last live instance is disposed), _instanceCount, _resourceAssemblies and _encodeFileNameKey.
// Two instances therefore interfere even though each is used from a single thread, and running
// these classes in parallel produced failures on a different, random SGBD each run.
//
// Serialising the assembly matches how the application uses the interpreter — one connection at a
// time — so this reflects a real constraint rather than hiding a test defect. See the note on
// EdiabasConnection.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
