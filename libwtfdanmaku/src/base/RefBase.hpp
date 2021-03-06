#ifndef _SMARTREF_REFBASE_HPP
#define _SMARTREF_REFBASE_HPP

#include <cstdint>
#include "Atomic.hpp"
#include "Noncopyable.hpp"

namespace xl {

    class RefBase {
    protected:
        explicit RefBase() : mRefCount(0) {}

        virtual ~RefBase() {
            mRefCount = 0;
        }
    public:
        void AddRef() {
            AtomicIncrease(&mRefCount);
        }

        void Release() {
            if (!AtomicDecrease(&mRefCount)) {
                delete this;
            }
        }
    private:
        DISALLOW_COPY_AND_ASSIGN(RefBase);
    private:
        mutable int32_t mRefCount;
    };


    class RefBaseNonAtomic {
    protected:
        explicit RefBaseNonAtomic() : mRefCount(0) {}

        virtual ~RefBaseNonAtomic() {
            mRefCount = 0;
        }
    public:
        void AddRef() {
            ++mRefCount;
        }

        void Release() {
            if (0 == --mRefCount) {
                delete this;
            }
        }
    private:
        DISALLOW_COPY_AND_ASSIGN(RefBaseNonAtomic);
    private:
        mutable int32_t mRefCount;
    };


}

#endif // _SMARTREF_REFBASE_HPP
