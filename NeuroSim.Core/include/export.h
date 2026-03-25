#pragma once

#ifdef _WIN32
#  ifdef NEUROSIM_CORE_EXPORTS
#    define NEUROSIM_API __declspec(dllexport)
#  else
#    define NEUROSIM_API __declspec(dllimport)
#  endif
#else
#  define NEUROSIM_API __attribute__((visibility("default")))
#endif
