/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.announce.type;

import belote.base.ComparableObject;

/**
 * AnnounceType class.
 * @author Dimitar Karamanov
 */
public abstract class AnnounceType extends ComparableObject {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -1500662241487451693L;

    /**
     * Announce type holder.
     */
    private final int type;

    /**
     * Announce type normal constant object.
     */
    public static final AnnounceType Normal = new Normal();

    /**
     * Announce type double constant object.
     */
    public static final AnnounceType Double = new Double();

    /**
     * Announce type redouble constant object.
     */
    public static final AnnounceType Redouble = new Redouble();

    /**
     * Constructor.
     * @param type announce type.
     */
    protected AnnounceType(final int type) {
        this.type = type;
    }

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with obfuscating. Don't use this method to represent the object. When the project is compiled with obfuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public final String toString() {
        return getClassShortName();
    }

    /**
     * Compares this announce type with the specified object(announce type) for order.
     * @param obj specified object (announce type).
     * @return int value which may be: = 0 if this announce type and the specified object(announce type) are equal > 0 if this announce type is bigger than the
     *         specified object(announce type) < 0 if this announce type is less than the specified object(announce type)
     */
    public final int compareTo(final Object obj) {
        final AnnounceType announceType = (AnnounceType) obj;
        return type - announceType.type;
    }

    /**
     * The method checks if this announce type and specified object (announce type) are equal.
     * @param obj specified object.
     * @return boolean true if this announce type is equal to specified object and false otherwise.
     */
    public final boolean equals(final Object obj) {
        if (obj instanceof AnnounceType) {
            final AnnounceType announceType = (AnnounceType) obj;
            return type == announceType.type;
        }
        return false;
    }

    /**
     * The method returns announce type hash code.
     * @return int announce type hash code value.
     */
    public final int hashCode() {
        return type;
    }
}
