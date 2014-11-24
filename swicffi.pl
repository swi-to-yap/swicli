/** <module> swicffi - Use C/C++ Runtimes from SWI-Prolog using only headers
%
% Dec 13, 2035
% Douglas Miles
*/

:-module(swicffi,[install_cffi/2,cffi_tests/0,to_forms/2,load_forms/1]).

:- use_module(swicli). 


:- style_check(-singleton).
:- style_check(-discontiguous).
:- set_prolog_flag(double_quotes, codes). 

/*
% TODO


defctype(PrologName,CType,Comment)  http://common-lisp.net/project/cffi/manual/html_node/defctype.html


6.1 Built-In Types

— Foreign Type: :char
— Foreign Type: :unsigned-char
— Foreign Type: :short
— Foreign Type: :unsigned-short
— Foreign Type: :int
— Foreign Type: :unsigned-int
— Foreign Type: :long
— Foreign Type: :unsigned-long
— Foreign Type: :long-long
— Foreign Type: :unsigned-long-long
These types correspond to the native C integer types according to the ABI of the Lisp implementation's host system.

:long-long and :unsigned-long-long are not supported natively on all implementations. However, they are emulated by mem-ref and mem-set.

When those types are not available, the symbol cffi-sys::no-long-long is pushed into *features*.

— Foreign Type: :uchar
— Foreign Type: :ushort
— Foreign Type: :uint
— Foreign Type: :ulong
— Foreign Type: :llong
— Foreign Type: :ullong
For convenience, the above types are provided as shortcuts for unsigned-char, unsigned-short, unsigned-int, unsigned-long, long-long and unsigned-long-long, respectively.

— Foreign Type: :int8
— Foreign Type: :uint8
— Foreign Type: :int16
— Foreign Type: :uint16
— Foreign Type: :int32
— Foreign Type: :uint32
— Foreign Type: :int64
— Foreign Type: :uint64
Foreign integer types of specific sizes, corresponding to the C types defined in stdint.h.

— Foreign Type: :float
— Foreign Type: :double
On all systems, the :float and :double types represent a C float and double, respectively. On most but not all systems, :float and :double represent a Lisp single-float and double-float, respectively. It is not so useful to consider the relationship between Lisp types and C types as isomorphic, as simply to recognize the relationship, and relative precision, among each respective category.

— Foreign Type: :long-double
This type is only supported on SCL.

— Foreign Type: :pointer &optional type
A foreign pointer to an object of any type, corresponding to void *. You can optionally specify type of pointer (e.g. (:pointer :char)). Although CFFI won't do anything with that information yet, it is useful for documentation purposes.

— Foreign Type: :void
No type at all. Only valid as the return type of a function.



struct person { int number; char* reason; };
  The equivalent defcstruct form follows:
(defcstruct person (number :int) (reason :string))


Dictionary

convert-from-foreign
convert-to-foreign
defbitfield
defcstruct
defcunion
defctype
defcenum
define-foreign-type
define-parse-method
foreign-bitfield-symbols
foreign-bitfield-value
foreign-enum-keyword
foreign-enum-value
foreign-slot-names
foreign-slot-offset
foreign-slot-pointer
foreign-slot-value
foreign-type-alignment
foreign-type-size
free-converted-object
free-translated-object
translate-from-foreign
translate-to-foreign
translate-into-foreign-memory
with-foreign-slots

*/
install_cffi(_Module,File):-read_file_to_codes(File,Codes,[]),to_forms(Codes,Forms),load_forms(Forms).

to_forms(String, Expr):- string(String),string_to_list(String,Codes),!,to_forms(Codes, Expr).
to_forms(Source,Program):-white(Source,Start), sexprs(Program,Start,[]),!.

test_run(Code):-to_forms(Code,Forms),load_forms(Forms).

load_forms(Forms):-forall(member(F,Forms),(writeq(F),nl)).


/* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
   Parsing (Using LISPy CFFI File format)
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - */


eoln(10).

blank --> [C], {C =< 32}, white.
blank --> ";", comment, white.

white --> blank.
white --> [].

comment --> [C], {eoln(C)}, !.
comment --> [C], comment.

sexprs([H|T]) --> sexpr(H), !, sexprs(T).
sexprs([]) --> [].

sexpr(L)                      --> "(", !, white, sexpr_list(L), white.
sexpr(vec(V))                 --> "#(", !, sexpr_vector(V), white.
sexpr(boo(t))                 --> "#t", !, white.
sexpr(boo(f))                 --> "#f", !, white.
sexpr(chr(N))                 --> "#\\", [C], !, {N is C}, white.
sexpr(str(S))                 --> """", !, sexpr_string(S), white.
sexpr([quote,E])              --> "'", !, white, sexpr(E).
sexpr([quasiquote,E])         --> "`", !, white, sexpr(E).
sexpr(['unquote-splicing',E]) --> ",@", !, white, sexpr(E).
sexpr([unquote,E])            --> ",", !, white, sexpr(E).
sexpr(E)                      --> sym_or_num(E), white.

sexpr_list([]) --> ")", !.
sexpr_list(_) --> ".", [C], {\+ sym_char(C)}, !, fail.
sexpr_list([Car|Cdr]) --> sexpr(Car), !, sexpr_rest(Cdr).

sexpr_rest([]) --> ")", !.
sexpr_rest(E) --> ".", [C], {\+ sym_char(C)}, !, sexpr(E,C), !, ")".
sexpr_rest([Car|Cdr]) --> sexpr(Car), !, sexpr_rest(Cdr).

sexpr_vector([]) --> ")", !.
sexpr_vector([First|Rest]) --> sexpr(First), !, sexpr_vector(Rest).

sexpr_string(Str) --> sexpr_ascii(Codes),{string_codes(Str,Codes)}.

sexpr_ascii([]) --> """", !.
sexpr_ascii([C|S]) --> chr(C), sexpr_ascii(S).

chr(92) --> "\\\\", !.
chr(34) --> "\\\"", !.
chr(N)  --> [C], {C >= 32, N is C}.

sym_or_num(E) --> [C], {sym_char(C)}, sym_string(S), {string_to_atom([C|S],E)}.

sym_string([H|T]) --> [H], {sym_char(H)}, sym_string(T).
sym_string([]) --> [].

number(N) --> unsigned_number(N).
number(N) --> "-", unsigned_number(M), {N is -M}.
number(N) --> "+", unsigned_number(N).

unsigned_number(N) --> digit(X), unsigned_number(X,N).
unsigned_number(N,M) --> digit(X), {Y is N*10+X}, unsigned_number(Y,M).
unsigned_number(N,N) --> [].

digit(N) --> [C], {C >= 48, C =<57, N is C-48}.

% . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .

sexpr(E,C,X,Z) :- white([C|X],Y), sexpr(E,Y,Z).

sym_char(C) :- C > 32, \+ member(C,";()#""',`").

string_to_atom(S,N) :- number(N,S,[]), !.
string_to_atom(S,I) :- lowcase(S,L), name(I,L).

lowcase([],[]).
lowcase([C1|T1],[C2|T2]) :- lowercase(C1,C2), lowcase(T1,T2).

lowercase(C1,C2) :- C1 >= 65, C1 =< 90, !, C2 is C1+32.
lowercase(C,C).


% Append:
reader_tests:- (test_run("
        (defun append (x y)
          (if x
              (cons (car x) (append (cdr x) y))
            y))

        (append '(a b) '(3 4 5))")).

    %@ V = [append, [a, b, 3, 4, 5]].
    

% Fibonacci, naive version:
reader_tests:- (test_run("
        (defun fib (n)
          (if (= 0 n)
              0
            (if (= 1 n)
                1
              (+ (fib (- n 1)) (fib (- n 2))))))
        (fib 24)")).

    %@ % 14,255,802 inferences, 3.71 CPU in 3.87 seconds (96% CPU, 3842534 Lips)
    %@ V = [fib, 46368].
    

% Fibonacci, accumulating version:
reader_tests:- (test_run("
        (defun fib (n)
          (if (= 0 n) 0 (fib1 0 1 1 n)))

        (defun fib1 (f1 f2 i to)
          (if (= i to)
              f2
            (fib1 f2 (+ f1 f2) (+ i 1) to)))

        (fib 250)")).

    %@ % 39,882 inferences, 0.010 CPU in 0.013 seconds (80% CPU, 3988200 Lips)
    %@ V = [fib, fib1, 7896325826131730509282738943634332893686268675876375].
    

% Fibonacci, iterative version:
reader_tests:- (test_run("
        (defun fib (n)
          (setq f (cons 0 1))
          (setq i 0)
          (while (< i n)
            (setq f (cons (cdr f) (+ (car f) (cdr f))))
            (setq i (+ i 1)))
          (car f))

        (fib 350)")).

    %@ % 34,233 inferences, 0.010 CPU in 0.010 seconds (98% CPU, 3423300 Lips)
    %@ V = [fib, 6254449428820551641549772190170184190608177514674331726439961915653414425].
    

% Higher-order programming and eval:
reader_tests:- test_run("
        (defun map (f xs)
          (if xs
              (cons (eval (list f (car xs))) (map f (cdr xs)))
            ()))
 ;;
        (defun plus1 (x) (+ 1 x))

        (map 'plus1 '(1 2 3))").

    %@ V = [map, plus1, [2, 3, 4]].


reader_tests:- test_run("(defcfun (:PL_query pl-query) :long (arg-1 :int))").
reader_tests:- test_run("(defcfun (\"PL_query\" pl-query) :long (arg-1 :int))").

cffi_tests :- install_cffi('snake-tail','cffi-tests/swi-prolog.cffi'),module(swicffi),prolog.


