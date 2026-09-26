namespace Integration.ViewModels;

public class ResponseVm : BaseAPIResponse
{
    public ResponseVm()
    {
    }

    public ResponseVm(ResultType resultType, string encryptedString)
    {
        ResultType = resultType;
        Data = encryptedString;
    }

    public ResponseVm(ResultType resultType, string message, string encryptedString)
    {
        ResultType = resultType;
        Message = message;
        Data = encryptedString;
    }

    public object Id { get; set; }
    public string StatusCode { get; set; }


    /// <summary>
    ///     Creates a successful response with a given result object
    /// </summary>
    /// <param name="message">The result object to return with the response</param>
    /// <returns>The response object</returns>
    public static ResponseVm Success(string message)
    {
        return new ResponseVm { ResultType = ResultType.Success, Message = message };
    }

    /// <summary>
    ///     Creates a successful response with a given result object
    /// </summary>
    /// <param name="message">The message to return with the response</param>
    /// <param name="result">The result object to return with the response</param>
    /// <returns>The response object</returns>
    public static ResponseVm Success<T>(string message, T result) where T : class
    {
        return new ResponseVm { ResultType = ResultType.Success, Message = message, Data = result };
    }

    /// <summary>
    ///     Creates a successful response with a given result object
    /// </summary>
    /// <param name="message">The result message to return with the response</param>
    /// <param name="id">The result id to return with the response</param>
    /// <returns>The response object</returns>
    public static ResponseVm Success(string message, string id)
    {
        return new ResponseVm { ResultType = ResultType.Success, Message = message, Data = id };
    }

    /// <summary>
    ///     Creates a successful response with a given result object
    /// </summary>
    /// <param name="message">The result message to return with the response</param>
    /// <param name="id">The result id to return with the response</param>
    /// <param name="result">The result object to return with the response</param>
    /// <returns>The response object</returns>
    public static ResponseVm Success<T>(string message, string id, T result)
    {
        return new ResponseVm { ResultType = ResultType.Success, Message = message, Id = id, Data = result };
    }

    /// <summary>
    ///     Creates a failed result. It takes no result object
    /// </summary>
    /// <param name="errorMessage">The error message returned with the response</param>
    /// <returns>The created response object</returns>
    public static ResponseVm Failed(string errorMessage)
    {
        return new ResponseVm { ResultType = ResultType.Error, Message = errorMessage };
    }

    /// <summary>
    ///     Creates a validation error response, indicating the input was invalid
    /// </summary>
    /// <param name="validationMessages">The validation message</param>
    /// <param name="message">The message</param>
    /// <returns>The Response object</returns>
    public static ResponseVm ValidationError(string message, List<string> validationMessages)
    {
        return new ResponseVm
        { ResultType = ResultType.ValidationError, Message = message, Errors = validationMessages };
    }

    public static ResponseVm Warning(string warningMessage)
    {
        var response = new ResponseVm { ResultType = ResultType.Warning, Message = warningMessage };

        return response;
    }
}
