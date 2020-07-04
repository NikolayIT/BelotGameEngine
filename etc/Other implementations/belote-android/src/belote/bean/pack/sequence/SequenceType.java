/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.sequence;

import belote.base.ComparableObject;

/**
 * SequenceType class.
 * @author Dimitar Karamanov
 */
public final class SequenceType extends ComparableObject {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 961246918139208205L;

    /**
     * Type for sequence 100.
     */
    private final static int TYPE_100 = 100;

    /**
     * Type for sequence 50.
     */
    private final static int TYPE_050 = 50;

    /**
     * Type for sequence 20.
     */
    private final static int TYPE_020 = 20;

    /**
     * Sequence 20 constant.
     */
    public final static SequenceType Quint = new SequenceType(TYPE_100);

    /**
     * Sequence 50 constant.
     */
    public final static SequenceType Quarte = new SequenceType(TYPE_050);

    /**
     * Sequence 100 constant.
     */
    public final static SequenceType Tierce = new SequenceType(TYPE_020);

    /**
     * Sequence type (20, 50, 100).
     */
    private final int type;

    /**
     * Constructor.
     * @param type sequence type.
     */
    private SequenceType(final int type) {
        this.type = type;
    }

    /**
     * Returns sequence's points.
     * @return sequence's points.
     */
    public int getSequencePoints() {
        return type;
    }

    /**
     * Compares this sequence type with the specified sequence type.
     * @param obj specified object.
     * @return int value which may be = 0 if this sequence type and the specified sequence type are equal > 0 if this sequence type is bigger than the specified
     *         sequence type < 0 if this sequence type is less than the specified sequence type
     */
    public int compareTo(final Object obj) {
        final SequenceType sequenceType = (SequenceType) obj;
        if (sequenceType.type < type) {
            return 1;
        }
        if (sequenceType.type > type) {
            return -1;
        }
        return 0;
    }

    /**
     * Returns hash code.
     * @return hash code.
     */
    public int hashCode() {
        return type;
    }

    /**
     * The method checks if this SequenceType and specified object (SequenceType) are equal.
     * @param obj specified object.
     * @return boolean true if this SequenceType is equal to specified object and false otherwise.
     */
    public boolean equals(final Object obj) {
        if (obj instanceof SequenceType) {
            final SequenceType sequenceType = (SequenceType) obj;
            return type == sequenceType.type;
        }
        return false;
    }

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with ofbuscating. Don't use this method to represent the object. When the project is compiled with ofbuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public String toString() {
        return String.valueOf(getSequencePoints()) + "(" + getClassShortName() + ")";
    }
}
