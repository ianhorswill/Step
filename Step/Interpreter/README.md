Here are the basics of how the interpreter works

* Modules hold values of global variables
* Tasks are objects that are bound to global variables
	* Primitive tasks are implemented in C#
	* All tasks written in user code are CompoundTasks
* A compound task is a set of methods to Try when the task is Tried (i.e. run)
* A method is a pattern to unify with the arguments, along with a chain of Step objects to Try when the method is Tried

Because we need to support backtracking, we never destroy any state information.  The operation for unifying two variables
doesn't overwrite the binding environment, it makes a new one.  Similarly, the operations for changing the value of a global,
or emitting text to the output buffer.  They always create a new state object that gets passed on to subsequent steps.
If those subsequent steps backtrack, then we throw the new state object away, and go back to using the old one.

The state objects that are passed along in the interpreter are:
* PartialOutput: this is just the buffer plus how much of it has been written.
  Writing to it means write to the end of the buffer, and return a new partial output with an updated length.
  If we backtrack, we throw away the version with the updated length.  We don't actually have to undo the
  writes to the buffer because they appear after what we're considering to be the "end" of the buffer.
* BindingEnvironment: this has the local frame for the current method (and array of LogicVariables), along with
  the binding lists for the logic variables that's maintained by the unifier.  It also contains the module that
  gives default values to global variables, and the dynamic state, which has the backtrackable updates to the
  global variables

These are both structs since we make a lot of them but we never need to store them in the heap.

Continuations are a little complicated.  Continuation means "the thing you should do when you finish with this."
In normal C# code, the continuation is your return address, along with the stack/frame pointers you're returning
to.  Conceptually, pattern matchers and pattern matched languages like this, have two continuations:
* The success continuation, which you involve if the current match operation succeeded.  This leads to the next thing
  to do in the current method, or in the calling method, if we're at the end of the current method.
* The fail continuation, which you invoke if this step fails.  This leads back to the last choice point in the system
  and gets it to start over with the next choice, or else invoke *its* fail continuation, if it's run out of choices.
When implementing using languages like Scheme that support tail-call optimization, you generally really implement it
two explicit continuations (explicit procedure parameters).  When implementing in machine code (as with Prolog 
compilers) or in C#, we need to do something fancier if we want to be efficient.

The scheme we use here is that the fail continuation is just the normal C# continuation.  So to fail, you just return.
In particular, you return false.  The succeed continuation is passed in as an explicit continuation parameter
(a delegate, i.e. a pointer to a procedure).  However, to reduce the number of closures (delgates) that get created
on the heap, we only call that continuation at the end of a method.  Within the method, the continuation is simply
the next step in the method's step chain.  Step.Continue() implements the check for whether we're at the end of
the chain or not and does the right thing.  So step implementations end by either returning false, if they failed,
or calling Continue(), if they succeeded.