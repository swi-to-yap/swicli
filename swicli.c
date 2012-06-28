/*
Linux: Compile with: 
 	  swipl-ld -shared -o ../../../bin/swicli.so swicli.c `pkg-config --cflags --libs mono-2` -lm
      swipl-ld -m32 -shared -o ../../../bin/swicli32.so swicli.c `pkg-config --cflags --libs mono-2` -lm
Windows: remember "Not Using Precompiled Headers"
          and compile this file as .cpp

*/
/*  $Id$

    Part of SWICLI - Bi-Directional Interface to .NET

    Author:        Douglas R. Miles
    E-mail:        logicmoo@gmail.com
    WWW:           http://www.logicmoo.com
    Copyright (C): 2010-2012, Logicmoo Developement

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

*/
#if defined(_MSC_VER) && !defined(WINDOWS_CPP)
#define WINDOWS_CPP 1
#endif

#ifndef WINDOWS_CPP
#include <mono/jit/jit.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/mono-config.h>
#include <stdlib.h>
#include <string.h>
typedef char gchar;
#endif //WINDOWS_CPP

#include "SWI-Prolog.h"
#include "stdafx.h"

#ifdef WINDOWS_CPP
extern "C" {
#endif //WINDOWS_CPP

	/// static MonoString* gimme () {   	return mono_string_new (mono_domain_get (), "All your monos are belong to us!"); }

	/// ?- load_foreign_library(swicli).
	/// This DLL shall have given: cli_load_lib(+AppDomainName, +AssemblyPartialName, +FullClassName, +StaticMethodName).
	/// used like: cli_load_lib('SWIProlog','SwiPlCs','SbsSW.SwiPlCs.swipl_win','install').
	foreign_t  cli_load_lib(term_t dname, term_t aname, term_t cname, term_t mname) 	
	{ 
		char *dnamestr;
		char *anamestr;
		char *cnamestr;
		char *mnamestr;
		if ( PL_get_atom_chars(dname, &dnamestr) && PL_get_atom_chars(aname, &anamestr) && PL_get_atom_chars(cname, &cnamestr) && PL_get_atom_chars(mname, &mnamestr) )
		{
#ifdef WINDOWS_CPP
			System::Reflection::Assembly^ assembly = System::Reflection::Assembly::Load(gcnew System::String(anamestr));
			if (assembly == nullptr) return PL_warning("No assembly found named %s", anamestr);
			System::Type^ type = assembly->GetType(gcnew System::String(cnamestr));
			if (type == nullptr) return PL_warning("No type found named %s", cnamestr);
			System::Reflection::MethodInfo^ method = type->GetMethod(gcnew System::String(mnamestr));
			if (method == nullptr) return PL_warning("No method found named %s", mnamestr);
			method->Invoke(nullptr, gcnew cli::array<System::Object^,1>(0));
#else        
			static int MonoInited = 0;
			static MonoDomain* domain;
			
			if (!MonoInited) {
				MonoInited = 1;
				/*
				* Load the default Mono configuration file, this is needed
				* if you are planning on using the dllmaps defined on the
				* system configuration
				*/
				mono_config_parse ("swicli.dll.config");

				/*  mono_jit_init() creates a domain: each assembly is loaded and run in a MonoDomain. */
				domain = mono_jit_init (dnamestr);

				/* add our special internal call, so that C# code can call us back. */
				//mono_add_internal_call ("MonoEmbed::gimme", gimme);
			}

			MonoImageOpenStatus status = MONO_IMAGE_OK;

			MonoAssembly* assembly = mono_assembly_load_with_partial_name (anamestr, &status);
			if (!assembly) return PL_warning("No assembly found named %s", anamestr);

			MonoImage* image = mono_assembly_get_image(assembly);
			if (!image) return PL_warning("No image module %s", anamestr);

			char str[255];
			strcpy(str,cnamestr);
			strcat(str,":");
			strcat(str,mnamestr);
			strcat(str,"()");

			MonoMethodDesc* desc = mono_method_desc_new(str,0);
			if (!desc) return PL_warning("No method desc %s", str);

			MonoMethod* method = mono_method_desc_search_in_image(desc,image);
			if (!method) return PL_warning("No method %s", str);

			void *args[0];
			mono_runtime_invoke(method, NULL, args, NULL);

			// we exit from our host app
			//retval = mono_environment_exitcode_get ();
			//mono_jit_cleanup (domain);

#endif //WINDOWS_CPP

			PL_succeed;
		}
		PL_fail;
	}

	// install_t == __declspec( dllexport )
	install_t install()
	{
		PL_register_foreign("cli_load_lib", 4, cli_load_lib, 0);
	}

#ifdef WINDOWS_CPP
}
#endif //WINDOWS_CPP

