= SWI-Prolog 2-Way interface to .NET =


== Introduction ==

# Provides SWI-Prolog full control of the Common Language Infrastructure.

# SwiCLI is a module that works on both Unix/Mac and Windows.

# cli_* preds loosely based on jpl_* interface of JPL

~ Copy/pasted much code from SwiPlCS by Uwe Lesta ~

# See library/swicli.pl for predicate list

== Installation ==

=== MS windows ===
Copy these two directories onto your Prolog Install Dir.

./bin/   
./library/


=== Linux OS/X ===
Copy these two directories onto your Prolog Install Dir

./lib/  
./library/


== Usage Examples ==

?- cli_new('System.Collections.Generic.List'('System.String'),[int],[10],Out).
Out = @'C#516939544'.

?- cli_get($Out,'Count',Out).
Out = 0.
?- cli_call($Obj,'Add'("foo"),Out).
Out = @void.
?- cli_call($Obj,'Add'("bar"),Out).
Out = @void.
?- cli_get($Out,'Count',Out).
Out = 2.
?- cli_col($Obj,E).
E = "foo" ;
E = "bar" ;
false.

32 ?- cli_get_type($Obj,Type),cli_get_typename(Type,Name).
Type = @'C#516939520',
Name = 'System.Collections.Generic.List`1[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]'.

?- cli_get_type($Obj,Type), cli_typespec(Type,Name).
Type = @'C#516939520',
Name = 'System.Collections.Generic.List'('String').

?- cli_shorttype(stringl,'System.Collections.Generic.List'('String')).
true.

 ?- cli_new(stringl,[],O).
O = @'C#516939472'.

?- cli_get_type($O,Type),cli_typespec(Type,Name).
Type = @'C#516939520',
Name = 'System.Collections.Generic.List'('String').
