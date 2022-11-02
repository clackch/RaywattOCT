#pragma once

#define GETSET(Type, MemberName, FaceName) \
            Type Get##FaceName() const { \
                return MemberName; \
          }; \
          void Set##FaceName(Type value) { \
                MemberName = value; \
          }

#define GETSETR(Type, MemberName, FaceName) \
          const Type &Get##FaceName() const { \
             return MemberName; \
          }; \
          void Set##FaceName(const Type &value) { \
             MemberName = value; \
          }

#define GET(Type, MemberName, FaceName) \
          Type Get##FaceName() const { \
             return MemberName; \
          }

#define GETR(Type, MemberName, FaceName) \
          const Type &Get##FaceName() const { \
             return MemberName; \
          }

#define GETRNC(Type, MemberName, FaceName) \
          Type &Get##FaceName() { \
             return MemberName; \
          }

#define SET(Type, MemberName, FaceName) \
            void Set##FaceName(const Type &value) { \
                MemberName = value; \
            }

// guarded versions

#define GETSETG(Type, MemberName, FaceName, GuardVal) \
            Type Get##FaceName() const { \
                    wxASSERT(GuardVal); \
                    return MemberName; \
          }; \
          void Set##FaceName(Type value) { \
                MemberName = value; \
          }

#define GETSETGR(Type, MemberName, FaceName, GuardVal) \
          const Type &Get##FaceName() const { \
             wxASSERT(GuardVal); \
             return MemberName; \
          }; \
          void Set##FaceName(const Type &value) { \
             MemberName = value; \
          }

#define GETG(Type, MemberName, FaceName, GuardVal) \
          Type Get##FaceName() const { \
             wxASSERT(GuardVal); \
                return MemberName; \
          }

#define GETGR(Type, MemberName, FaceName, GuardVal) \
          const Type &Get##FaceName() const { \
             wxASSERT(GuardVal); \
             return MemberName; \
          }

#define SETG(Type, MemberName, FaceName, GuardVal) \
            void Set##FaceName(const Type &value) { \
                wxASSERT(GuardVal); \
                MemberName = value; \
            }