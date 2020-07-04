package com.karamanov.framework;

import java.io.Serializable;

public final class BooleanFlag implements Serializable {
	
	/**
	 * Serializable constant.
	 */
	private static final long serialVersionUID = 4218960544131511719L;
	
	private Boolean value;
	
	public BooleanFlag() {
		this(Boolean.TRUE);
	}
	
	public BooleanFlag(Boolean value) {
		this.value = value;
	}
	
	public void setTrue() {
		value = Boolean.TRUE;
	}
	
	public void setFalse() {
		value = Boolean.FALSE;
	}
	
	public boolean getValue() {
		return value.booleanValue();
	}
}