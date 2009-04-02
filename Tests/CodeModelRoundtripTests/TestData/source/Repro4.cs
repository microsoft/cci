using System.Collections;
using System.Collections.Generic;

public abstract class A<TLeftInput, TRightInput, TOutput> {

    public class B {

        public class C<TLeftKey> {

            private D<TLeftInput, TLeftKey> field = null;

            public void Receive() {
                field.Foo(); // <<<< Emits the wrong field!
            }
        }
    }

} // class

public class D<TElement, TKey> {

    internal void Foo() {
        throw new System.NotImplementedException();
    }

} // class

// L_0007: ldfld class System.Linq.Parallel.PartitionedStream`2<!0, !6> System.Linq.Parallel.BinaryQueryOperator`3/BinaryQueryOperatorResults/RightChildResultsRecipient<!TLeftInput, !TRightInput, !TOutput, !TLeftKey>::m_leftPartitionedStream
//                                                       HERE !!!    ^ <----- Here!!!! Should be '3'
// L_0007: ldfld class System.Linq.Parallel.PartitionedStream`2<!0, !3> System.Linq.Parallel.BinaryQueryOperator`3/BinaryQueryOperatorResults/RightChildResultsRecipient<!TLeftInput, !TRightInput, !TOutput, !TLeftKey>::m_leftPartitionedStream
