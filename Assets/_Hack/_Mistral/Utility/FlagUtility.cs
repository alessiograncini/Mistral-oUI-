using System.Collections.Generic;
public class Condition<Z>
{
    protected Z _value;
    protected Z _lastValue;


    public Z Value
    {
        get
        {
            return _value;
        }
        set
        {
            _lastValue = _value;
            _value = value;
        

        }
    }
    public Z LastValue
    {
        get
        {
            return _lastValue;
        }
    }
    public bool HasChanged
    {
        get
        {
            return !EqualityComparer<Z>.Default.Equals(_value, _lastValue);
        }
    }
}

public class BoolCondition : Condition<bool>
{
    public BoolCondition()
    {
        _value = false;
        _lastValue = false;
    }
    public BoolCondition(bool value)
    {
        _value = value;
        _lastValue = value;
    }
    public bool ChangedTrue
    {
        get { return _value && HasChanged; }
    }
    public bool ChangedFalse
    {
        get { return !_value && HasChanged; }
    }
}