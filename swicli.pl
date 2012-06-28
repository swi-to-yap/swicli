/*  $Id$
*  
*  Project: Swicli.Library - Two Way Interface for .NET and MONO to SWI-Prolog
*  Author:        Douglas R. Miles
*  E-mail:        logicmoo@gmail.com
*  WWW:           http://www.logicmoo.com
*  Copyright (C):  2010-2012 LogicMOO Developement
*
*  This library is free software; you can redistribute it and/or
*  modify it under the terms of the GNU Lesser General Public
*  License as published by the Free Software Foundation; either
*  version 2.1 of the License, or (at your option) any later version.
*
*  This library is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
*  Lesser General Public License for more details.
*
*  You should have received a copy of the GNU Lesser General Public
*  License along with this library; if not, write to the Free Software
*  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*
*********************************************************/

:- module(swicli,
          [
            module_functor/4,
            to_string/2,
            member_elipse/2
          ]).

/** <module> SWI-Prolog 2-Way interface to .NET/Mono

*Introduction*

This is an overview of an interface which allows SWI-Prolog programs to dynamically create and manipulate .NET objects. 

Here are some significant features of the interface and its implementation: 

* API is similar to that of XPCE: the four main interface calls are cli_new, cli_call, cli_set and cli_get (there is a single cli_free, though .NET's garbage collection is extended transparently into Prolog) 
* Uses @/1 to construct representations of certain .NET values; if  @/1 is defined as a prefix operator (as used by XPCE), then you can write @false, @true, @null etc. in your source code; otherwise (and for portability) you'll have to write e.g. @(true) etc. 
* cli_call/4 (modeled from JPL's jpl_call/4) resolves overloaded methods automatically and dynamically, inferring the types of the call's actual parameters, and identifying the most specific of the applicable method implementations (similarly, cli_new resolves overloaded constructors)
* Completely dynamic: no precompilation is required to manipulate any .NET classes which can be found at run time, and any objects which can be instantiated from them 
* Interoperable with SwiPlCS's .NET API (which has evolved from Uwe Lesta's SwiPlCS) 
* Exploits the Invocation API of the .NET P/Invoke Interface: this is a mandatory feature of any compliant .NET 
* Implemented with a fair amount of C# code and Prolog code in one module (swicli.pl)  (which I believe to be ISO Standard Prolog compliant and portable) and a SWI-Prolog-specific foreign library (swicli[32].dll for Windows and swicli[32].so *nix), implemented in ANSI C but making a lot of use of the SWI-Prolog Foreign Language Interface Then uses Swicli.Library.dll (Managed binary) that runs on both Mono and .NET runtimes. 
* the Prolog-calls-CLI (mine) and CLI-calls-Prolog (Ewe's) parts of SWICLI are largely independent; mine concentrates on representing all .NET data values and objects within Prolog, and supporting manipulation of objects; Ewe's concentrates on representing any Prolog term within .NET, and supporting the calling of goals within Prolog and the retrieving of results back into .NET 
* @(terms) are canonical (two references are ==/2 equal if-and-only-if they refer to the same object within the .NET) 
* are represented as structures containing a distinctive atom so as to exploit SWI-Prolog's atom garbage collection: when an object reference is garbage-collected in Prolog, the .NET garbage collector is informed, so there is sound and complete overall garbage collection of .NET objects within the combined Prolog+.NET system 
* .NET class methods can be called by name: SWICLI invisibly fetches (and caches) essential details of method invocation, exploiting .NET Reflection facilities 
* Reason about the types of .NET data values, object references, fields and methods: SWICLI supports a canonical representation of all .NET types as structured terms (e.g. array(array(byte))) and also as atomic .NET signatures 
* when called from Prolog, void methods return a @(void) value (which is distinct from all other SWICLI values and references) 
* Tested on Windows XP, Windows7 and Fedora Linux, but is believed to be readily portable to SWI-Prolog on other platforms as far as is feasible, .NET data values and object references are represented within Prolog canonically and without loss of information (minor exceptions: .NET float and double values are both converted to Prolog float values; .NET byte, char, short, int and long values are all converted to Prolog integer values; the type distinctions which are lost are normally of no significance) 
* Requires .NET 2.0 and class libraries (although it doesn't depend on any .NET 2-specific facilities, and originally was developed for use with both 1.0 thru 4.0 .NETs, I haven't tested it with 1.0 recently, and don't support this) 


==
?- use_module(library(swicli)).

?- cli_call('System.Threading.ThreadPool','GetAvailableThreads'(X,Y),_).

X=499, Y=1000

==
Doc root will be findable from http://code.google.com/p/opensim4opencog/wiki/SwiCLI


@see	CSharp.txt
	
@author	Douglas Miles

*/
/*
-------
=====
" 
format's
**/


%% cwl(+StringValue) is det.
% allas for System.Console.WriteLine(+String)   (not user_output but what .NET thinks its System.Console.Out)

%% link_swiplcs(+PathName) is det.
%

%% cli_typespec(+ClazzSpec,-Value) is det.
% coerces a ClazzSpec to a Value representing a TypeSpec term

%% cli_to_from_layout(+ClazzSpec,+MemberSpec,+ToSpec) is det.
%

%% cli_add_layout(+ClazzSpec,+MemberSpec) is det.
%

%% cli_to_from_recomposer(+ClazzSpec,+MemberSpec,+Obj2r,+R2obj) is det.
%

%% test_out(+Incoming,+ByrefInt32Outbound) is det.
%

%% test_opt(+Incoming,+StringOptionalstr,+ByrefInt32Outbound) is det.
%

%% test_opt(+Incoming,+StringOptionalstr,+ByrefInt32Outbound) is det.
%

%% test_ref(+Incoming,+ByrefStringOptionalstr,+ByrefInt32Outbound) is det.
%

%% test_ref(+Incoming,+ByrefStringOptionalstr,+ByrefInt32Outbound) is det.
%

%% test_var_arg(+ByrefInt32Outbound,+ArrayOfInt32Incoming) is det.
%

%% cli_find_constructor(+ClazzSpec,+MemberSpec,-Method) is det.
%

%% cli_new(+ClazzSpec,+MemberSpec,+Param,-Value) is det.
%

%% cli_new_array(+ClazzSpec,+Rank,-Value) is det.
%

%% cli_lock_enter(+LockObj) is det.
%

%% cli_lock_exit(+LockObj) is det.
%

%% cli_find_method(+ClazzOrInstance,+MemberSpec,-Method) is det.
%

%% cli_call_raw(+ClazzOrInstance,+MemberSpec,+Params,-Value) is det.
%



%% cli_get_raw(+ClazzOrInstance,+MemberSpec,-Value) is det.
%

%% cli_get_property(+ClazzOrInstance,+MemberSpec,+IndexValues,-Value) is det.
%

%% cli_set_raw(+ClazzOrInstance,+MemberSpec,+Param) is det.
%

%% cli_to_str_raw(+Obj,+Str) is det.
%

%% cli_java_to_string(+Param,-Value) is det.
%

%% cli_props_for_type(+ClazzSpec,+MemberSpecs) is det.
%

%% cli_members(+ClazzOrInstance,-Members) is det.
%

%% cli_member_doc(+Memb,+Doc,+Xml) is det.
%

%% cli_getterm(+ValueCol,+Value,-Value) is det.
%

%% cli_cast(+Value,+ClazzSpec,-Value) is det.
%

%% cli_cast_immediate(+Value,+ClazzSpec,-Value) is det.
%

%% pl_list_to_casted_array(+IEnumerablePlTermTerm,+ArrayOfParameterInfo,+ByrefActionTodo) is det.
%

%% pl_list_to_casted_array(+Skip,+IEnumerablePlTermTerm,+ArrayOfParameterInfo,+ByrefActionTodo) is det.
%

%% cli_test_pbd(+Pred,+Counted) is det.
%

%% cli_test_pbdt(+Pred,+Counted) is det.
%

%% cli_test_pbct(+Pred,+Counted) is det.
%

%% cli_test_pbc(+Pred,+Counted) is det.
%

%% cli_load_type(+TypeT) is det.
%

%% cli_find_type(+ClazzSpec,+ClassRef) is det.
%

%% cli_find_class(+ClazzName,-ClazzObject) is det.
%

%% cli_get_type(+Value,-Value) is det.
%

%% cli_get_class(+Value,-Value) is det.
%

%% cli_class_from_type(+Value,-Value) is det.
%

%% cli_type_from_class(+Value,-Value) is det.
%

%% cli_shorttype(+ValueName,+Value) is det.
%

%% cli_get_classname(+Value,-Value) is det.
%

%% cli_get_type_fullname(+Value,-Value) is det.
%

%% cli_new_delegate(+DelegateClass,+PrologPred,-Value) is det.
%

%% cli_delegate_term(+TypeFi,+PrologPred,+BooleanSaveKey) is det.
%

%% cli_array_to_term(+ArrayValue,-Value) is det.
%

%% cli_test_array_to_term1(-Value) is det.
%

%% cli_test_array_to_term2(-Value) is det.
%

%% cli_array_to_termlist(+ArrayValue,-Value) is det.
%

%% cli_term_to_array(+ArrayValue,-Value) is det.
%


%% cli_add_tag(+TaggedObj,+TagString) is det.
%  lowlevel access to create a tag name 

%% cli_remove_tag(+TagString) is det.
%  lowlevel access to remove a tag name

%% cli_to_tagged(+Obj,+Str) is det.
%  return a @(Str) version of the object 

%% cli_immediate_object(+Immediate,-Value) is det.
%  return a @(Value) version of the Immediate value 

%% cli_tracker_begin(-Tracker) is det.
%  Return a Tracker ref and all objects created from this point can be released via cli_tracker_free/1

%% cli_tracker_free(+Tracker) is det.
%  @see cli_tracker_begin/1

%% cli_free(+TaggedObject) is det.
%  remove a TaggedObject from the heap

%% cli_heap(+TaggedObject) is det.
%  Pin a TaggedObject onto the heap



%% cli_throw(+Ex) is det.
%

%% cli_break(+Ex) is det.
%

:- push_operators([op(600, fx, ('*'))]).

:-dynamic(shortTypeName/2).
:-dynamic(cli_subproperty/2).
:-module_transparent(cli_subproperty/2).

:-set_prolog_flag(double_quotes,string).

:-module_transparent(shortTypeName/2).
%%:-module_transparent(cli_get/3).


% Load C++ DLL

:-dynamic(loadedcli_Assembly/0).

foName1(X):- current_prolog_flag(address_bits,32) -> X = swicli32 ;  X= swicli.
foName(Y):-foName1(X), (current_prolog_flag(unix,true) -> Y= foreign(X); Y =X).

loadcli_Assembly:-loadedcli_Assembly,!.
loadcli_Assembly:-assert(loadedcli_Assembly),fail.
loadcli_Assembly:- foName(SWICLI),strip_module(SWICLI,_,DLL),load_foreign_library(DLL).
:-loadcli_Assembly.

cli_halt:-cli_halt(0).
cli_halt(_Status):-cli_call('Swicli.Library.PrologClient','ManagedHalt',_).


onWindows:-current_prolog_flag(arch,ARCH),atomic_list_concat([_,_],'win',ARCH).

%------------------------------------------------------------------------------

% cli_load_lib(+AppDomainName, +AssemblyPartialName, +FullClassName, +StaticMethodName).
%  The C++ DLL should have given us cli_load_lib/4
%  remember to: export LD_LIBRARY_PATH=/development/opensim4opencog/bin:/development/opensim4opencog/lib/x86_64-linux:$LD_LIBRARY_PATH
%  should have given us cli_load_assembly/1
:- cli_load_lib('SWIProlog','Swicli.Library','Swicli.Library.Embedded','install').

%------------------------------------------------------------------------------

%% cli_load_assembly(+Term1) is det.
% the cli_load_assembly/1 should have give us a few more cli_<Predicates>
:- cli_load_assembly('Swicli.Library').

%------------------------------------------------------------------------------

%%  cli_is_type(+Impl,?Type) is det.
%
% tests to see if the Impl Object is assignable to Type
%
cli_is_type(Impl,Type):-not(ground(Impl)),nonvar(Type),!,attach_console,trace,cli_find_type(Type,RealType),cli_call(RealType,'IsInstanceOfType'(object),[Impl],'@'(true)).
cli_is_type(Impl,Type):-nonvar(Type),cli_find_type(Type,RealType),!,cli_call(RealType,'IsInstanceOfType'(object),[Impl],'@'(true)).
cli_is_type(Impl,Type):-cli_get_type(Impl,Type).

%------------------------------------------------------------------------------

%% cli_subclass(+Subclass,+Superclass) 
% tests to see if the Subclass is assignable to Superclass

cli_subclass(Sub,Sup):-cli_find_type(Sub,RealSub),cli_find_type(Sup,RealSup),cli_call(RealSup,'IsAssignableFrom'('System.Type'),[RealSub],'@'(true)).

%------------------------------------------------------------------------------

%% cli_col(+Col,-Elem) 
% iterates out Elems for Col

% old version:s cli_collection(Obj,Ele):-cli_call(Obj,'ToArray',[],Array),cli_array_to_term_args(Array,Vect),!,arg(_,Vect,Ele).
cli_collection(Error,_Ele):-cli_is_null(Error),!,fail.
cli_collection([S|Obj],Ele):-!,member(Ele,[S|Obj]).
cli_collection(Obj,Ele):-
      cli_memb(Obj,m(_, 'GetEnumerator', _, [], [], _, _)),!,
      cli_call(Obj,'GetEnumerator',[],Enum),!,
      call_cleanup(cli_enumerator_element(Enum,Ele),cli_free(Enum)).
cli_collection(Obj,Ele):-cli_array_to_term_args(Obj,Vect),!,arg(_,Vect,Ele).
cli_collection(Obj,Ele):-cli_memb(Obj,m(_, 'ToArray', _, [], [], _, _)),cli_call(Obj,'ToArray',[],Array),cli_array_to_term_args(Array,Vect),!,arg(_,Vect,Ele).
cli_collection(Obj,Ele):-cli_array_to_termlist(Obj,Vect),!,member(Ele,Vect).

cli_col(X,Y):-cli_collection(X,Y).

%------------------------------------------------------------------------------

cli_array_to_term_args(Array,Term):-cli_array_to_term(Array,array(_,Term)).

%------------------------------------------------------------------------------

%% cli_col_add(+Col,+Item)
% add an Item to Col
cli_col_add(Col,Value):-cli_call(Col,'Add'(Value),_).

%% cli_col_contains(+Col,+Item)
% Test an Item in Col
cli_col_contains(Col,Value):-cli_call(Col,'Contains'(Value),_).

%% cli_col_remove(+Col,+Item)
% Remove an Item in Col
cli_col_remove(Col,Value):-cli_call(Col,'Remove'(Value),_).

%% cli_col_removeall(+Col)
% Clears a Col
cli_col_removeall(Col):-cli_call(Col,'Clear',_).

%% cli_col_size(+Col,?Count)
% Returns the Count
cli_col_size(Col,Count):-cli_call(Col,'Count',Count).

%% cli_write(+Obj)
%  writes an object out
cli_write(S):-cli_to_str(S,W),writeq(W).

%% cli_writeln(+Obj)
%  writes an object out with a new line
cli_writeln(S):-cli_write(S),nl.


cli_fmt(WID,String,Args):-cli_fmt(String,Args),cli_free(WID). % WID will be made again each call
cli_fmt(String,Args):-cli_call('System.String','Format'('string','object[]'),[String,Args],Result),cli_writeln(Result).


%% cli_to_str(+Obj,-String) 
% Resolves inner @(Obj)s to strings

cli_to_str(Term,String):-catch(ignore(cli_to_str0(Term,String0)),_,true),copy_term(String0,String),numbervars(String,666,_).
cli_to_str0(Term,Term):- not(compound(Term)),!.
cli_to_str0(Term,String):-Term='@'(_),cli_is_object(Term),catch(cli_to_str_raw(Term,String),_,Term==String),!.
cli_to_str0([A|B],[AS|BS]):-!,cli_to_str0(A,AS),cli_to_str0(B,BS).
cli_to_str0(eval(Call),String):-nonvar(Call),!,call(Call,Result),cli_to_str0(Result,String).
cli_to_str0(Term,String):-Term=..[F|A],cli_to_str0(A,AS),String=..[F|AS],!.
cli_to_str0(Term,Term).


%% cli_new_prolog_collection(+PredImpl,+ElementType,-PBD)

cli_new_prolog_collection(PredImpl,TypeSpec,PBC):-
   module_functor(PredImpl,Module,Pred,_),
   atom_concat(Pred,'_get',GET),atom_concat(Pred,'_add',ADD),atom_concat(Pred,'_remove',REM),atom_concat(Pred,'_clear',CLR),
   PANON =..[Pred,_],PGET =..[GET,Val],PADD =..[ADD,Val],PREM =..[REM,Val],PDYN =..[Pred,Val],
   asserta(( PGET :- PDYN )),
   asserta(( PADD :- assert(PDYN) )),
   asserta(( PREM :- retract(PDYN) )),
   asserta(( CLR :- retractall(PANON) )),
   cli_new('Swicli.Library.PrologBackedCollection'(TypeSpec),0,
      [Module,GET,ADD,REM,CLR],PBC).

module_functor(PredImpl,Module,Pred,Arity):-strip_module(PredImpl,Module,NewPredImpl),strip_arity(NewPredImpl,Pred,Arity).
strip_arity(Pred/Arity,Pred,Arity).
strip_arity(PredImpl,Pred,Arity):-functor(PredImpl,Pred,Arity).


%% cli_new_prolog_dictionary(+PredImpl,+KeyType,+ValueType,-PBD)

cli_new_prolog_dictionary(PredImpl,KeyType,ValueType,PBD):-
   cli_new_prolog_collection(PredImpl,KeyType,PBC),
   module_functor(PredImpl,Module,Pred,_),
   atom_concat(Pred,'_get',GET),atom_concat(Pred,'_set',SET),atom_concat(Pred,'_remove',REM),atom_concat(Pred,'_clear',CLR),
   PANON =..[Pred,_,_],PGET =..[GET,Key,Val], PSET =..[SET,Key,Val],PREM =..[REM,Val],PDYN =..[Pred,Key,Val],
   asserta(( PGET :- PDYN )),
   asserta(( PSET :- assert(PDYN) )),
   asserta(( PREM :- retract(PDYN) )),
   asserta(( CLR :- retractall(PANON) )),
   cli_new('Swicli.Library.PrologBackedDictionary'(KeyType,ValueType),0,
      [Module,GET,PBC,SET,REM,CLR],PBD).

% %%%%%%%%%%%%%%%%5555555555555555555555555555555555555555555555555555555555
/* EXAMPLE: How to turn current_prolog_flag/2 into a PrologBacked dictionary
% %%%%%%%%%%%%%%%%5555555555555555555555555555555555555555555555555555555555

Here is the webdocs:

create_prolog_flag(+Key, +Value, +Options)                         [YAP]
    Create  a  new Prolog  flag.    The ISO  standard does  not  foresee
    creation  of  new flags,  but many  libraries  introduce new  flags.

current_prolog_flag(?Key, -Value)    
    Get system configuration parameters

set_prolog_flag(:Key, +Value)                                      [ISO]
    Define  a new  Prolog flag or  change its value.   


It has most of the makings of a "PrologBackedDictionary"  but first we need a 
PrologBackedCollection to produce keys

% %%%%%%%%%%%%%%%%5555555555555555555555555555555555555555555555555555555555
% First we'll need a conveinence predicate add_new_flag/1  for adding new flags for the collection
% %%%%%%%%%%%%%%%%5555555555555555555555555555555555555555555555555555555555

?- asserta(( add_new_flag(Flag):- create_prolog_flag(Flag,_,[access(read_write),type(term)])   )).

?- asserta(( current_pl_flag(Flag):- current_prolog_flag(Flag,_)   )).

% %%%%%%%%%%%%%%%%5555555555555555555555555555555555555555555555555555555555
% Next we'll use the add_new_flag/1 in our PrologBackedCollection
% %%%%%%%%%%%%%%%%5555555555555555555555555555555555555555555555555555555555
?- context_module(Module),cli_new('Swicli.Library.PrologBackedCollection'(string),0,[Module,current_pl_flag,add_new_flag,@(null),@(null)],PBC).

% meaning:
       %% 'Swicli.Library.PrologBackedCollection'(string) ==> Type of object it returs to .NET is System.String
       %% 0 ==> First (only) constructor
       %% Module ==> user
       %% current_pl_flag ==> use current_pl_flag/1 for our GETTER of Items
       %% add_new_flag ==> Our Adder(Item) (defined in previous section)
       %% @(null) ==> No Remover(Item) 
       %% @(null) ==> No clearer
       %% PBC ==> Our newly created .NET ICollection<string>

% by nulls in the last two we've created a partially ReadOnly ICollection wexcept we can add keys


% %%%%%%%%%%%%%%%%5555555555555555555555555555555555555555555555555555555555
% Now we have a Keys collection let us declare the Dictionary (our intial objective)
% %%%%%%%%%%%%%%%%5555555555555555555555555555555555555555555555555555555555
?- context_module(Module), cli_new('Swicli.Library.PrologBackedDictionary'(string,string),0,
           [Module,current_prolog_flag,$PBC,set_prolog_flag,@(null),@(null)],PBD).

       %% 'Swicli.Library.PrologBackedDictionary'(string) ==> Type of Key,Value it returns to .NET are System.Strings
       %% 0 ==> First (only) constructor
       %% Module ==> user
       %% current_prolog_flag ==> use current_prolog_flag/2 is a GETTER.
       %% $PBC ==> Our Key Maker from above
       %% set_prolog_flag/2 ==> our SETTER(Key,ITem)
       %% @(null) ==> No Remover(Key,Value) 
       %% @(null) ==> No clearer
       %% PBD ==> Our newly created .NET IDictionary<string,string>

% %%%%%%%%%%%%%%%%5555555555555555555555555555555555555555555555555555555555
% Now we have a have a PrologBackedDictionary in $PBD
% so let us play with it
% %%%%%%%%%%%%%%%%5555555555555555555555555555555555555555555555555555555555

%% is there a key named foo?

?- current_pl_flag(foo).
No.

%% Add a value to the Dictionanry
?- cli_map_add($PBD,foo,bar).
Yes.

%% set if there is a proper side effect
?- current_pl_flag(foo).
Yes.

?- current_prolog_flag(foo,X).
X = bar.
Yes.

?- cli_map($PBD,foo,X).
X = bar.
Yes.

?- cli_call($PBD,'ContainsKey'(foo),X).
X = @true.



%% iterate the Dictionary
?- cli_map($PBD,K,V).



*/

cli_demo(PBC,PBD):- asserta(( add_new_flag(Flag) :- create_prolog_flag(Flag,_,[access(read_write),type(term)])   )),
   asserta(( current_pl_flag(Flag):- current_prolog_flag(Flag,_)   )),
   context_module(Module),cli_new('Swicli.Library.PrologBackedCollection'(string),0,[Module,current_pl_flag,add_new_flag,@(null),@(null)],PBC),
   cli_new('Swicli.Library.PrologBackedDictionary'(string,string),0,[Module,current_prolog_flag,PBC,set_prolog_flag,@(null),@(null)],PBD).




%% cli_is_null(+Obj)
% is null or void

cli_is_null(Obj):-once(Obj='@'(null);Obj='@'(void)).



%% cli_non_obj(+Obj) 
% is null or void or var

cli_non_obj(Obj):-once(var(Obj);(Obj='@'(null));(Obj='@'(void))).


%% cli_non_null(+Obj)
% is not null or void

cli_non_null(Obj):- \+(cli_is_null(Obj)).


cli_is_true(Obj):- Obj == @(true).
cli_true(@(true)).



cli_is_false(Obj):- Obj== @(false).
cli_false(@(false)).



cli_is_void(Obj):- Obj== @(void).
cli_void(@(void)).



cli_is_type(Obj):-nonvar(Obj),cli_is_type(Obj,'System.Type').




%% cli_is_object(+Obj) is det.
%
% is Object a CLR object and not null or void (includes struct,enum,object,event)

cli_is_object([_|_]):-!,fail.
cli_is_object('@'(O)):-!,O\=void,O\=null.
cli_is_object(O):-functor(O,F,_),memberchk(F,[struct,enum,object,event]).


%% cli_is_tagged(+Obj) 
% is Object a tagged object and not null or void (excludes struct,enum,object,event)

cli_is_tagged([_|_]):-!,fail.
cli_is_tagged('@'(O)):- O\=void,O\=null.



%% cli_memb(O,F,X) 
% Object to the member infos of it

cli_memb(O,X):-cli_members(O,Y),member(X,Y).
cli_memb(O,F,X):-cli_memb(O,X),member(F,[f,p, c,m ,e]),functor(X,F,_).





%% cli_new_event_waiter(+ClazzOrInstance,+MemberSpec,-BlockOn) is det.
%

%% cli_add_event_waiter(+BlockOn,+ClazzOrInstance,+MemberSpec,-NewBlockOn) is det.
%

%% cli_block_until_event(+BlockOn,+MaxTime,+TestVarsCode,-ExitCode) is det.
%
% cli_block_until_event/3 use Foriegnly defined cli_block_until_event/4 and Dispose.

%% cli_block_until_event(+WaitOn,+Time,+Lambda) is det.
%
cli_block_until_event(WaitOn,Time,Lambda):-setup_call_cleanup(true,cli_block_until_event(WaitOn,Time,Lambda,_),cli_call(WaitOn,'Dispose',_)).


%% cli_raise_event_handler(+ClazzOrInstance,+MemberSpec,+Param,-Value) is det.
%

%% cli_add_event_handler(+Term1,+Arity,+IntPtrControl,Pred) is det.
% @see cli_add_event_handler/4

%% cli_add_event_handler(+ClazzOrInstance,+MemberSpec,+PrologPred) is det.
% Create a .NET Delegate that calls PrologPred when MemberSpec is called

%% cli_remove_event_handler(+ClazzOrInstance,+MemberSpec,+PrologPred) is det.
%

/*

ADDING A NEW EVENT HOOK

We already at least know that the object we want to hook is found via our call to

?- botget(['Self'],AM).

So we ask for the e/7 (event handlers of the members)

?- botget(['Self'],AM),cli_memb(AM,e(A,B,C,D,E,F,G)). 

 Press ;;;; a few times until you find the event Name you need (in the B var)

A = 6,                                          % index number
B = 'IM',                                       % event name
C = 'System.EventHandler'('InstantMessageEventArgs'),   % the delegation type
D = ['Object', 'InstantMessageEventArgs'],      % the parameter types (2)
E = [],                                         % the generic paramters
F = decl(static(false), 'AgentManager'),        % the static/non staticness.. the declaring class
G = access_pafv(true, false, false, false)      % the PAFV bits

So reading the parameter types  "['Object', 'InstantMessageEventArgs']" lets you know the pred needs at least two arguments
And "F = decl(static(false), 'AgentManager')" says add on extra argument at from for Origin

So registering the event is done:

?- botget(['Self'],AM), cli_add_event_handler(AM,'IM',handle_im(_Origin,_Object,_InstantMessageEventArgs))

To target a predicate like 

handle_im(Origin,Obj,IM):-writeq(handle_im(Origin,Obj,IM)),nl.



*/


/*

?- cli_new(array(string),[int],[32],O),cli_add_tag(O,'string32').

?- cli_get_type(@(string32),T),cli_writeln(T).

*/



%% cli_map(Map,?Key,?Value).

cli_map(Map,Key,Value):-nonvar(Key),!,cli_call(Map,'TryGetValue',[Key,Value],@(true)).
cli_map(Map,Key,Value):-cli_col(Map,Ele),cli_get(Ele,'Key',Key),cli_get(Ele,'Value',Value).
cli_map_set(Map,Key,Value):-cli_call(Map,'[]'(type(Key)),[Key,Value],_).
cli_map_add(Map,Key,Value):-cli_call(Map,'Add'(Key,Value),_).
cli_map_remove(Map,Key,Value):-cli_map(Map,Key,Value),!,cli_call(Map,'Remove'(Key),_).
cli_map_removeall(Map):-cli_call(Map,'Clear',_).
cli_map_size(Map,Count):-cli_call(Map,'Count',Count).


%% cli_Preserve(TF,Calls)

cli_preserve(TF,Calls):-
   cli_get('Swicli.Library.PrologClient','PreserveObjectType',O),
   call_cleanup((cli_set('Swicli.Library.PrologClient','PreserveObjectType',TF),Calls),
   cli_set('Swicli.Library.PrologClient','PreserveObjectType',O)).


%% cli_with_collection(Calls).  
%%%%%% as tagged objects are created they are tracked .. when the call is complete any new object tags are released

cli_with_collection(Calls):-cli_tracker_begin(O),call_cleanup(Calls,cli_tracker_free(O)).

cli_array_to_length(Array,Length):-cli_get(Array,'Length',Length).

/*

?- cli_new(array(string),[int],[32],O),cli_array_to_length(O,L),cli_array_to_term(O,T).
O = @'C#861856064',
L = 32,
T = array('String', values(@null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null, @null)).
*/

%% cli_array_to_list(+Arg1,+Arg2) is det.
%
cli_array_to_list(Array,List):-cli_array_to_term(Array,array(_,Term)),Term=..[_|List].

%% cli_new(+X, +Params, -V).

% ?- cli_load_assembly('IKVM.OpenJDK.Core')
% ?- cli_new('java.lang.Long'(long),[44],Out),cli_to_str(Out,Str).
% same as..
% ?- cli_new('java.lang.Long',[long],[44],Out),cli_to_str(Out,Str).
% arity 4 exists to specify generic types
% ?- cli_new('System.Int64',[int],[44],Out),cli_to_str(Out,Str).
% ?- cli_new('System.Text.StringBuilder',[string],["hi there"],Out),cli_to_str(Out,Str).
%
%   X can be:
%    * an atomic classname
%       e.g. 'java.lang.String'
%    * an atomic descriptor
%       e.g. '[I' or 'Ljava.lang.String;'
%    * a suitable type
%       i.e. any class(_,_) or array(_)
%
%   if X is an object (non-array)  type   or  descriptor and Params is a
%   list of values or references, then V  is the result of an invocation
%   of  that  type's  most  specifically-typed    constructor  to  whose
%   respective formal parameters the actual   Params are assignable (and
%   assigned)
%
%   if X is an array type or descriptor   and Params is a list of values
%   or references, each of which is   (independently)  assignable to the
%   array element type, then V is a  new   array  of as many elements as
%   Params has members,  initialised  with   the  respective  members of
%   Params;
%
%   if X is an array type  or   descriptor  and Params is a non-negative
%   integer N, then V is a new array of that type, with N elements, each
%   initialised to Java's appropriate default value for the type;
%
%   If V is {Term} then we attempt to convert a new jpl.Term instance to
%   a corresponding term; this is of  little   obvious  use here, but is
%   consistent with jpl_call/4 and jpl_get/3


cli_new(Clazz,ConstArgs,Out):-Clazz=..[BasicType|ParmSpc],cli_new(BasicType,ParmSpc,ConstArgs,Out).
%%cli_new(ClazzConstArgs,Out):-ClazzConstArgs=..[BasicType|ConstArgs],cli_new(BasicType,ConstArgs,ConstArgs,Out).
/*

 Make a "new string[32]" and get it's length.

 ?- cli_new(array(string),[int],[32],O),cli_get(O,'Length',L).

*/
/*
 NOTES

 ?- cli_new('System.Int32'(int),[44],Out),cli_to_str(Out,Str).
ERROR: Trying to constuct a primitive type
ERROR: Cant find constructor [int] on System.Int32
   Fail: (8) swicli:cli_new('System.Int32', [int], [44], _G731) ? abort
% Execution Aborted
*/


%% cli_call(+Obj, +CallTerm, -Result).
%% cli_call(+X, +MethodSpec, +Params, -Result).
%
%   X should be:
%     an object reference
%       (for static or instance methods)
%     a classname, descriptor or type
%       (for static methods of the denoted class)
%
%   MethodSpec should be:
%     a method name (as an atom)
%       (may involve dynamic overload resolution based on inferred types of params)
%
%   Params should be:
%     a proper list (perhaps empty) of suitable actual parameters for the named method
%
%   finally, an attempt will be made to unify Result with the returned result


cli_call(Obj,[Prop|CallTerm],Out):-cli_get(Obj,Prop,Mid),!,cli_call(Mid,CallTerm,Out).
cli_call(Obj,CallTerm,Out):-CallTerm=..[MethodName|Args],cli_call(Obj,MethodName,Args,Out).

% arity 4
cli_call(Obj,[Prop|CallTerm],Params,Out):-cli_get(Obj,Prop,Mid),!,cli_call(Mid,CallTerm,Params,Out).
cli_call(Obj,MethodSpec,Params,Out):-cli_call_raw(Obj,MethodSpec,Params,Out_raw),!,cli_unify(Out,Out_raw).



%% cli_libCall(+CallTerm, -Out).

cli_lib_call(CallTerm,Out):-cli_call('Swicli.Library.PrologClient',CallTerm,Out).


member_elipse(NV,{NVs}):-!,member_elipse(NV,NVs).
member_elipse(NV,(A,B)):-!,(member_elipse(NV,A);member_elipse(NV,B)).
member_elipse(NV,NV).

cli_expand(eval(Call),Result):-nonvar(Call),!,call(Call,Result).
cli_expand(Value,Value).


%% cli_get(+X, +Fspec, -V)

%
%   X can be:
%     * a classname, a descriptor, or an (object or array) type
%       (for static fields);
%     * a non-array object
%       (for static and non-static fields)
%     * an array
%       (for 'length' pseudo field, or indexed element retrieval),
%   but not:
%     * a String
%       (clashes with class name; anyway, String has no fields to retrieve)
%
%   Fspec can be:
%       * an atomic field name,
%       * or an integral array index (to get an element from an array,
%	* or a pair I-J of integers (to get a subrange (slice?) of an
%	  array)
%       * A list of  [a,b(1),c] to denoate cli_getting X.a.b(1).c
%
%   finally, an attempt will be made to unify V with the retrieved value

cli_get(Obj,NVs):-forall(member_elipse(N=V,NVs),cli_get(Obj,N,V)).

cli_get(Obj,_,_):-cli_non_obj(Obj),!,fail.
cli_get(Obj,[P],Value):-!,cli_get(Obj,P,Value).
cli_get(Obj,[P|N],Value):-!,cli_get(Obj,P,M),cli_get(M,N,Value),!.
cli_get(Obj,P,ValueOut):-cli_getOverloaded(Obj,P,Value),!,cli_unify(Value,ValueOut).

cli_getOverloaded(Obj,_,_):-cli_non_obj(Obj),!,fail,throw(cli_non_obj(Obj)).
cli_getOverloaded(Obj,P,Value):-cli_getHook(Obj,P,Value),!.
cli_getOverloaded(Obj,P,Value):-compound(P),!,cli_call(Obj,P,Value),!.
cli_getOverloaded(Obj,P,Value):-cli_get_raw(Obj,P,Value),!.
cli_getOverloaded(Obj,P,Value):-not(atom(Obj)),cli_get_type(Obj,CType),!,cli_get_typeSubProps(CType,Sub),cli_get_rawS(Obj,Sub,SubValue),cli_getOverloaded(SubValue,P,Value),!.

cli_get_rawS(Obj,[P],Value):-!,cli_get_rawS(Obj,P,Value).
cli_get_rawS(Obj,[P|N],Value):-!,cli_get_rawS(Obj,P,M),cli_get_rawS(M,N,Value),!.
cli_get_rawS(Obj,P,Value):-cli_get_raw(Obj,P,Value),!.

%%cli_get_typeSubProps(CType,Sub):-cli_ProppedType(
cli_get_typeSubProps(CType,Sub):-cli_subproperty(Type,Sub),cli_subclass(CType,Type).

:-dynamic(cli_getHook/3).
:-multifile(cli_getHook/3).


%% cli_set(+Obj, +PropTerm, +NewValue).

cli_set(Obj,NVs):-forall(member_elipse(N=V,NVs),cli_set(Obj,N,V)).

cli_set(Obj,_,_):-cli_non_obj(Obj),!,fail.
cli_set(Obj,[P],Value):-!,cli_set(Obj,P,Value).
cli_set(Obj,[P|N],Value):-!,cli_get(Obj,P,M),cli_set(M,N,Value),!.
cli_set(Obj,P,Value):-cli_setOverloaded(Obj,P,Value).

cli_setOverloaded(Obj,_,_):- cli_non_obj(Obj),!,fail.
cli_setOverloaded(Obj,P,ValueI):-cli_expand(ValueI,Value),ValueI \== Value,!,cli_setOverloaded(Obj,P,Value).
cli_setOverloaded(Obj,P,Value):-cli_setHook(Obj,P,Value),!.
cli_setOverloaded(Obj,P,Value):-cli_subproperty(Type,Sub),cli_is_type(Obj,Type),cli_get_rawS(Obj,Sub,SubValue),cli_setOverloaded(SubValue,P,Value),!.
cli_setOverloaded(Obj,P,Value):-cli_set_raw(Obj,P,Value),!.

:-dynamic(cli_setHook/3).
:-multifile(cli_setHook/3).



%% cli_unify(OE,PE)

cli_unify(OE,PE):-OE=PE,!.
cli_unify(enum(_,O1),O2):-!,cli_unify(O1,O2).
cli_unify(O2,enum(_,O1)):-!,cli_unify(O1,O2).
cli_unify(eval(O1),O2):-cli_expand(O1,O11),!,cli_unify(O11,O2).
cli_unify(O2,eval(O1)):-cli_expand(O1,O11),!,cli_unify(O11,O2).
cli_unify(O1,O2):-atomic(O1),atomic(O2),string_to_atom(S1,O1),string_to_atom(S2,O2),!,S1==S2.
cli_unify([O1|ARGS1],[O2|ARGS2]):-!,cli_unify(O1,O2),cli_unify(ARGS1,ARGS2).
cli_unify(O1,O2):-cli_is_tagged(O1),cli_to_str(O1,S1),!,cli_unify(O2,S1).
cli_unify(O1,O2):-O1=..[F|[A1|RGS1]],!,O2=..[F|[A2|RGS2]],cli_unify([A1|RGS1],[A2|RGS2]).

%type   jpl_iterator_element(object, datum)

% jpl_iterator_element(+Iterator, -Element) :-

cli_iterator_element(I, E) :- cli_is_type(I,'java.util.Iterator'),!,
	(   cli_call(I, hasNext, [], @(true))
	->  (   cli_call(I, next, [], E)        % surely it's steadfast...
	;   cli_iterator_element(I, E)
	)
	).

cli_enumerator_element(I, _E) :- cli_call_raw(I, 'MoveNext', [], @(false)),!,fail.
cli_enumerator_element(I, E) :- cli_get(I, 'Current', E).
cli_enumerator_element(I, E) :- cli_enumerator_element(I, E).

old_cli_enumerator_element(I, E) :- %%cli_is_type('System.Collections.IEnumerator',I),!,
	(   cli_call_raw(I, 'MoveNext', [], @(true))
	->  (   cli_get(I, 'Current', E)        % surely it's steadfast...
	;   cli_enumerator_element(I, E)
	)
	).



cli_to_data(Term,String):- cli_new('System.Collections.Generic.List'(object),[],[],Objs),cli_to_data(Objs,Term,String).
cli_to_data(_,Term,Term):- not(compound(Term)),!.
%%cli_to_data(_Objs,[A|B],[A|B]):-!.
cli_to_data(_Objs,[A|B],[A|B]):-'\+' '\+' A=[_=_],!.
cli_to_data(Objs,[A|B],[AS|BS]):-!,cli_to_data(Objs,A,AS),cli_to_data(Objs,B,BS).
cli_to_data(Objs,Term,String):-cli_is_tagged(Term),!,cli_gettermData(Objs,Term,Mid),(Term==Mid-> true; cli_to_data(Objs,Mid,String)).
cli_to_data(Objs,Term,FAS):-Term=..[F|A],cli_to_data1(Objs,F,A,Term,FAS).

cli_to_data1(_Objs,struct,_A,Term,Term):-!.
cli_to_data1(_Objs,object,_A,Term,Term):-!.
cli_to_data1(_Objs,enum,_A,Term,Term):-!.

cli_to_data1(Objs,F,A,_Term,String):-cli_to_data(Objs,A,AS),!,String=..[F|AS].

cli_gettermData(Objs,Term,String):-cli_get_type(Term,Type),cli_props_for_type(Type,Props),cli_getMap(Objs,Term,Props,Name,Value,Name=Value,Mid),!,cli_to_data(Objs,Mid,String).
cli_gettermData(Objs,Term,Mid):-cli_getterm(Objs,Term,Mid),!.


cli_getMap(Objs,Term,_,_,_,_,List):- cli_is_type(Term,'System.Collections.IEnumerable'),findall(ED,(cli_col(Term,E),cli_to_data(Objs,E,ED)),List),!.
cli_getMap(Objs,Term,Props,Name,Value,NameValue,List):-cli_getMap1(Objs,Term,Props,Name,Value,NameValue,List).

cli_getMap1(Objs,Term,Props,Name,Value,NameValue,List):-findall(NameValue,(member(Name,Props),cli_get_raw(Term,Name,ValueM),cli_to_data(Objs,ValueM,Value)),List).


%% cli_with_lock(+Lock,+Call)
% 
%  Lock the first arg while calling Call

cli_with_lock(Lock,Call):-setup_call_cleanup(cli_lock_enter(Lock),Call,cli_lock_exit(Lock)).



%% cli_with_gc(+Call)
%
% use Forienly defined cli_tracker_begin/1 and cli_tracker_free/1

cli_with_gc(Call):-setup_call_cleanup(cli_tracker_begin(Mark),Call,cli_tracker_free(Mark)).



%% cli_make_list(+Arg1,+Arg2,+Arg3) is det.
% @see  cli_new_list_1/2

%% cli_new_list_1(+Arg1,+Arg2,+Arg3) is det.
% @see cli_make_list/2

cli_new_list_1(Item,Type,List):-cli_new('System.Collections.Generic.List'(Type),[],[],List),cli_call(List,add(Item),_).
cli_make_list(Items,Type,List):-cli_new('System.Collections.Generic.List'(Type),[],[],List),forall(member(Item,Items),cli_call(List,add(Item),_)).


%% cli_sublist(+Mask,+List)
%  Test to see if Mask appears in List

cli_sublist(What,What):-!.
cli_sublist(Mask,What):-append(Pre,_,What),append(_,Mask,Pre).


% cli_debug/[1,2]


cli_debug(format(Format,Args)):-atom(Format),sformat(S,Format,Args),!,cli_debug(S).
cli_debug(Data):-format(user_error,'~n %% cli_-DEBUG: ~q~n',[Data]),flush_output(user_error).

%%cli_debug(Engine,Data):- format(user_error,'~n %% ENGINE-DEBUG: ~q',[Engine]),cli_debug(Data).

%%to_string(Object,String):-jpl_is_ref(Object),!,jpl_call(Object,toString,[],String).
to_string(Object,String):-cli_to_str(Object,String).


% cli_intern/3

:-dynamic(cli_interned/3).
:-multifile(cli_interned/3).
:-module_transparent(cli_interned/3).
cli_intern(Engine,Name,Value):-retractall(cli_interned(Engine,Name,_)),assert(cli_interned(Engine,Name,Value)),cli_debug(cli_interned(Name,Value)),!.



% cli_eval/3


:-dynamic(cli_eval_hook/3).
:-multifile(cli_eval_hook/3).
:-module_transparent(cli_eval_hook/3).

cli_eval(Engine,Name,Value):- cli_eval_hook(Engine,Name,Value),!,cli_debug(cli_eval(Engine,Name,Value)),!.
cli_eval(Engine,Name,Value):- Value=cli_eval(Engine,Name),cli_debug(cli_eval(Name,Value)),!.
cli_eval_hook(Engine,In,Out):- catch(call((In,Out=In)),E,Out= foobar(Engine,In,E)).
cli_is_defined(_Engine,Name):-cli_debug(cli_not_is_defined(Name)),!,fail.
cli_getSymbol(Engine,Name,Value):- (cli_interned(Engine,Name,Value);Value=cli_UnDefined(Name)),!,cli_debug(cli_getSymbol(Name,Value)),!.

%:-use_module(library(jpl)).
%:-use_module(library(pce)).

%:-interactor.

%% cli_hide(+Pred) is det.
% hide Pred from tracing

to_pi(M:F/A,M:PI):-functor(PI,F,A),!.
to_pi(F/A,M:PI):-context_module(M),functor(PI,F,A),!.
to_pi(M:PI,M:PI):-!.
to_pi(PI,M:PI):-context_module(M).
cli_hide(PIn):-to_pi(PIn,Pred),
   '$set_predicate_attribute'(Pred, trace, 1),
   '$set_predicate_attribute'(Pred, noprofile, 1),
   '$set_predicate_attribute'(Pred, hide_childs, 1).

:-meta_predicate(cli_notrace(0)).

%% cli_notrace(+Call) is nondet.
%  use call/1 with trace turned off
cli_notrace(Call):-tracing,notrace,!,call_cleanup(call(Call),trace).
cli_notrace(Call):-call(Call).

:-forall((current_predicate(swicli:F/A),atom_concat(cli_,_,F)),(export(F/A),functor(P,F,A),cli_hide(swicli:P))).


%% cli_new_delegate(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_true(+Arg1) is det.
%

%% member_elipse(+Arg1,+Arg2) is det.
%

%% cli_setHook(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_get_typeSubProps(+Arg1,+Arg2) is det.
%

%% cli_type_from_class(+Arg1,+Arg2) is det.
%

%% cli_subclass(+Arg1,+Arg2) is det.
%


%% cli_expand(+Arg1,+Arg2) is det.
%

%% cli_find_class(+Arg1,+Arg2) is det.
%

%% cli_add_layout(+Arg1,+Arg2) is det.
%


%% cli_new_prolog_dictionary(+Arg1,+Arg2,+Arg3,+Arg4) is det.
%


%% cli_cast_immediate(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_writeln(+Arg1) is det.
%

%% cli_subproperty(+Arg1,+Arg2) is det.
%

%% cli_map_size(+Arg1,+Arg2) is det.
%

%% cli_members(+Arg1,+Arg2) is det.
%

%% cli_halt(+Arg1) is det.
%

%% cli_col_removeall(+Arg1) is det.
%

%% cli_map_add(+Arg1,+Arg2,+Arg3) is det.
%


%% cli_set_raw(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_col_contains(+Arg1,+Arg2) is det.
%

%% cli_call_raw(+Arg1,+Arg2,+Arg3,+Arg4) is det.
%

%% cli_void(+Arg1) is det.
%

%% cli_to_data(+Arg1,+Arg2) is det.
%

%% cli_col(+Arg1,+Arg2) is det.
%

%% cli_test_array_to_term2(+Arg1) is det.
%

%% cli_to_data1(+Arg1,+Arg2,+Arg3,+Arg4,+Arg5) is det.
%

%% cli_get_type_fullname(+Arg1,+Arg2) is det.
%

%% cli_is_true(+Arg1) is det.
%

%% cli_lock_enter(+Arg1) is det.
%

%% cli_memb(+Arg1,+Arg2) is det.
%

%% cli_class_from_type(+Arg1,+Arg2) is det.
%

%% cli_get_rawS(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_getSymbol(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_find_type(+Arg1,+Arg2) is det.
%

%% cli_get(+Arg1,+Arg2) is det.
%

%% cli_to_from_recomposer(+Arg1,+Arg2,+Arg3,+Arg4) is det.
%

%% cli_test_pbdt(+Arg1,+Arg2) is det.
%

%% cli_new(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_intern(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_to_str0(+Arg1,+Arg2) is det.
%

%% cli_load_lib(+Arg1,+Arg2,+Arg3,+Arg4) is det.
%

%% cli_map_removeall(+Arg1) is det.
%

%% cli_sublist(+Arg1,+Arg2) is det.
%

%% cli_fmt(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_heap(+Arg1) is det.
%

%% cli_member_doc(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_immediate_object(+Arg1,+Arg2) is det.
%

%% cli_col_size(+Arg1,+Arg2) is det.
%

%% cli_to_str_raw(+Arg1,+Arg2) is det.
%

%% cli_memb(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_getMap(+Arg1,+Arg2,+Arg3,+Arg4,+Arg5,+Arg6,+Arg7) is det.
%

%% cli_is_void(+Arg1) is det.
%


%% cli_test_array_to_term1(+Arg1) is det.
%

%% cli_enumerator_element(+Arg1,+Arg2) is det.
%

%% cli_set(+Arg1,+Arg2) is det.
%

%% cli_false(+Arg1) is det.
%

%% cli_find_constructor(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_get_classname(+Arg1,+Arg2) is det.
%

%% module_functor(+Arg1,+Arg2,+Arg3,+Arg4) is det.
%

%% cli_collection(+Arg1,+Arg2) is det.
%

%% cli_non_null(+Arg1) is det.
%

%% cli_getOverloaded(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_get_class(+Arg1,+Arg2) is det.
%

%% cli_load_type(+Arg1) is det.
%

%% cli_demo(+Arg1,+Arg2) is det.
%

%% cli_eval(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_unify(+Arg1,+Arg2) is det.
%


%% cli_with_collection(+Arg1) is det.
%

%% cli_interned(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_typespec(+Arg1,+Arg2) is det.
%

%% cli_call(+Arg1,+Arg2,+Arg3,+Arg4) is det.
%

%% cli_test_pbd(+Arg1,+Arg2) is det.
%

%% cli_throw(+Arg1) is det.
%

%% cli_set(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_free(+Arg1) is det.
%

%% cli_fmt(+Arg1,+Arg2) is det.
%

%% cli_new_list_1(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_getMap1(+Arg1,+Arg2,+Arg3,+Arg4,+Arg5,+Arg6,+Arg7) is det.
%

%% cli_getterm(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_map_remove(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_call(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_java_to_string(+Arg1,+Arg2) is det.
%

%% cli_to_tagged(+Arg1,+Arg2) is det.
%

%% cli_write(+Arg1) is det.
%

%% cli_map_set(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_col_remove(+Arg1,+Arg2) is det.
%

%% cli_to_data(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_get_raw(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_term_to_array(+Arg1,+Arg2) is det.
%

%% cli_col_add(+Arg1,+Arg2) is det.
%

%% cli_lock_exit(+Arg1) is det.
%

%% cli_iterator_element(+Arg1,+Arg2) is det.
%


%% cli_array_to_term(+Arg1,+Arg2) is det.
%

%% cli_array_to_term_args(+Arg1,+Arg2) is det.
%

%% cli_setOverloaded(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_shorttype(+Arg1,+Arg2) is det.
%

%% to_string(+Arg1,+Arg2) is det.
%

%% cli_is_false(+Arg1) is det.
%

%% cli_new(+Arg1,+Arg2,+Arg3,+Arg4) is det.
%

%% cli_is_defined(+Arg1,+Arg2) is det.
%

%% cli_getHook(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_get_type(+Arg1,+Arg2) is det.
%

%% cli_is_null(+Arg1) is det.
%

%% cli_non_obj(+Arg1) is det.
%

%% cli_remove_event_handler(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_lib_call(+Arg1,+Arg2) is det.
%

%% cli_eval_hook(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_new_prolog_collection(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_debug(+Arg1) is det.
%

%% cli_break(+Arg1) is det.
%

%% cli_to_from_layout(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_array_to_length(+Arg1,+Arg2) is det.
%

%% cli_preserve(+Arg1,+Arg2) is det.
%

%% cli_tracker_free(+Arg1) is det.
%

%% cli_cast(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_to_str(+Arg1,+Arg2) is det.
%

%% cli_remove_tag(+Arg1) is det.
%

%% cli_halt is det.
%

%% cli_props_for_type(+Arg1,+Arg2) is det.
%

%% cli_with_lock(+Arg1,+Arg2) is det.
%

%% cli_delegate_term(+Arg1,+Arg2,+Arg3,+Arg4) is det.
%

%% cli_get(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_array_to_termlist(+Arg1,+Arg2) is det.
%

%% cli_gettermData(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_get_property(+Arg1,+Arg2,+Arg3,+Arg4) is det.
%

%% cli_find_method(+Arg1,+Arg2,+Arg3) is det.
%

%% cli_is_type(+Arg1) is det. 
%

end_of_file.




15 ?- cli_to_tagged(sbyte(127),O),cli_get_type(O,T),cli_writeln(O is T).
"127"is"System.SByte"
O = @'C#283319280',
T = @'C#283324332'.

16 ?- cli_to_tagged(long(127),O),cli_get_type(O,T),cli_writeln(O is T).
"127"is"System.Int64"
O = @'C#283345876',
T = @'C#283345868'.

17 ?- cli_to_tagged(ulong(127),O),cli_get_type(O,T),cli_writeln(O is T).
"127"is"System.UInt64"
O = @'C#283346772',
T = @'C#283346760'.

15 ?- cli_to_tagged(sbyte(127),O),cli_get_type(O,T),cli_writeln(O is T).
"127"is"System.SByte"
O = @'C#283319280',
T = @'C#283324332'.

16 ?- cli_to_tagged(long(127),O),cli_get_type(O,T),cli_writeln(O is T).
"127"is"System.Int64"
O = @'C#283345876',
T = @'C#283345868'.

18 ?- cli_to_tagged(343434127,O),cli_get_type(O,T),cli_writeln(O is T).
"343434127"is"System.Int32"
O = @'C#281925284',
T = @'C#281925280'.

19 ?- cli_to_tagged(3434341271,O),cli_get_type(O,T),cli_writeln(O is T).
"3434341271"is"System.UInt64"
O = @'C#281926616',
T = @'C#283346760'.

21 ?- cli_to_tagged(343434127111,O),cli_get_type(O,T),cli_writeln(O is T).
"343434127111"is"System.UInt64"
O = @'C#281930092',
T = @'C#283346760'.

28 ?- cli_to_tagged(34343412711111111111111111111111111111,O),cli_get_type(O,T),cli_writeln(O is T).
"34343412711111111111111111111111111111"is"java.math.BigInteger"
O = @'C#281813796',
T = @'C#281810860'.

?- cli_call('System.Environment','Version',X),cli_writeln(X).
"2.0.50727.5448"
X = @'C#499252128'.
