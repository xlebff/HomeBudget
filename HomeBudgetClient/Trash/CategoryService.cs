//using HomeBudgetShared.Contracts;
//using HomeBudgetShared.Models;

//namespace HomeBudgetClient.Services
//{
//    public interface ICategoryService
//    {
//        Task<List<CategoryResponse>> GetCategoriesAsync();
//        Task<CategoryResponse?> GetCategoryAsync(Guid id);
//        Task<CategoryResponse?> CreateCategoryAsync(CreateCategoryRequest request);
//        Task<CategoryResponse?> UpdateCategoryAsync(Guid id, Category category);
//        Task<bool> DeleteCategoryAsync(Guid id);
//    }

//    public class CategoryService : ICategoryService
//    {
//        private readonly ApiClient _apiClient;

//        public CategoryService(ApiClient apiClient)
//        {
//            _apiClient = apiClient;
//        }

//        public async Task<List<CategoryResponse>> GetCategoriesAsync()
//        {
//            return await _apiClient.GetAsync<List<CategoryResponse>>("categories") ?? new List<CategoryResponse>();
//        }

//        public async Task<CategoryResponse?> GetCategoryAsync(Guid id)
//        {
//            return await _apiClient.GetAsync<CategoryResponse>($"categories/{id}");
//        }

//        public async Task<CategoryResponse?> CreateCategoryAsync(CreateCategoryRequest request)
//        {
//            return await _apiClient.PostAsync<CreateCategoryRequest, CategoryResponse>("categories", request);
//        }

//        public async Task<CategoryResponse?> UpdateCategoryAsync(Guid id, Category category)
//        {
//            return await _apiClient.PutAsync<Category, CategoryResponse>($"categories/{id}", category);
//        }

//        public async Task<bool> DeleteCategoryAsync(Guid id)
//        {
//            return await _apiClient.DeleteAsync($"categories/{id}");
//        }
//    }
//}
