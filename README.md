STEP is a simple HTN planner for generating text.  It's essentially a very fancy template system.

It's intended for use in Unity games, but you could in principle use it in any project that compiled to .NET or Mono.

Here are the basics:
* A *task* is like a procedure.  You call it and it makes text.  It can take arguments.
* A *method* is a way of generating the text for a task.  When a task is called, it tries each method until it finds one that works.  Methods can call other tasks.
* Tasks and methods can *fail*.
  * When a method calls a subtask and that subtask fails, the method fails.  (It's actually a little more complicated than this, since you can have backtracking within a method, but we can ignore that for now)
  * When a method fails, the task it is part of tries the next method.
  * If all the methods for a task fail, then the task fails.
* Methods are called with pattern matching, a la Prolog.  The system only tries those methods whose pattern matches the arguments in the call.
* Task names are always capitalized.
* Variable names should start with a `?` (so e.g. `?x`, `?y`)

**Examples**

The basic definition is the name of a task, a colon, and the text it expands to:

    Test: This is a test

When `Test` is called, this will generate the text "This is a test".

Calls to subtasks are wrapped in square brackets:

    Dog: the happy dog
    Test: [Dog] loves [Dog]

Calling this `Test` generates "the happy dog loves the happy dog".

Tasks can take arguments, and those arguments can even be other tasks:

    Speaker subject: I
    Speaker object: me
    Listener subject: you
    Listener object: you
    Love ?x ?y: [?x subject] love [?y object]
    Test: [Love Speaker Listener]

When `Test` is called, this will generate the text "I love you".

For long chunks of text, you can have multiline method definitions.  These have just the task name, arguments and colon on the first line, then the body, then `[end]`.  Within the body, newlines are ignored, but double newlines generate an actual newline in the output.

    Opening:
	A long time ago in a galaxy far, far away....

	Star Wars

	Episode IV

	A NEW HOPE 
	
	It is a period of civil war. Rebel spaceships, striking from a hidden base,
	have won their first victory against the evil Galactic Empire. During the
	battle, Rebel spies managed to steal secret plans to the Empire’s ultimate
	weapon, the DEATH STAR, an armored space station with enough power to destroy
	an entire planet. Pursued by the Empire’s sinister agents, Princess Leia 
	races home aboard her starship, custodian of the stolen plans that can save
	her people and restore freedom to the galaxy....
	[end]

Here, `Opening` is the name of the task, and the rest is the text it should generate when called.